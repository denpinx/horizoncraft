using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Expand;
using horizoncraft.script.Services;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.chunk;

/// <summary>
/// 区块服务基类
/// 默认实现了所有方法
/// </summary>
public partial class ChunkServiceBase : ServiceBase, IDisposable, ISave
{
    /// <summary>
    /// 下标映射贴图坐标集
    /// </summary>
    protected readonly Vector2I[] _terrainCoord =
    [
        new Vector2I(3, 3), //无0
        new Vector2I(3, 2), //下1
        new Vector2I(3, 0), //上2
        new Vector2I(3, 1), //上下3
        new Vector2I(2, 3), //右4
        new Vector2I(2, 2), //左上10  
        new Vector2I(2, 0), //右上6
        new Vector2I(2, 1), //上下左11
        new Vector2I(0, 3), //左8
        new Vector2I(0, 2), //左下9
        new Vector2I(0, 0), //右下相同5
        new Vector2I(0, 1), //上下右7
        new Vector2I(1, 3), //左右12
        new Vector2I(1, 2), //上左右13
        new Vector2I(1, 0), //下左右
        new Vector2I(1, 1) //全部相同15
    ];

    public enum LightModeEnum
    {
        /// <summary>
        /// 禁用光照更新
        /// </summary>
        None,

        /// <summary>
        /// 光线投射模式
        /// </summary>
        RayCastMode,

        /// <summary>
        /// 深度优先模式
        /// </summary>
        DFSMode
    }

    /// <summary>
    /// 光照模式
    /// </summary>
    public LightModeEnum LightMode = LightModeEnum.DFSMode;

    /// <summary>
    /// 默认光源光照强度
    /// </summary>
    protected int LightSize = 8;

    /// <summary>
    /// 默认天空光照强度
    /// </summary>
    protected int SkyLight = 8;

    /// <summary>
    /// 区块加载事件
    /// </summary>
    public Action<Chunk> OnChunkLoaded;

    /// <summary>
    /// 区块保存事件
    /// </summary>
    public Action<Chunk> OnChunkSaving;

    /// <summary>
    /// 已加载区块集合
    /// </summary>
    public ConcurrentDictionary<Vector2I, Chunk> Chunks = new();

    /// <summary>
    /// 待加载区块的请求集合
    /// </summary>
    public ConcurrentQueue<Vector2I> LoadChunkQueue = new();

    /// <summary>
    /// 区块加载视距半径
    /// </summary>
    public int LoadHorizon = 1;

    protected CancellationTokenSource _tokenSource;
    protected Task _processLoadTask;

    #region 性能分析

    protected Stopwatch _stopwatchTick = new Stopwatch();
    protected Stopwatch _stopwatchTick_GroupingTime_ = new Stopwatch();

    protected long tickConsumed;
    protected double tickConsumed_μs;
    protected double GroupingTime_μs;

    protected int processloadtask_count = 0;

    #endregion


    public ChunkServiceBase(World world) : base(world)
    {
        world.timer.Timeout += Ticking;

        PlayerNode.GetInformation[nameof(ChunkServiceBase)] =
            () => $"加载区块:{Chunks.Count}\n" +
                  $"待加载区块:{LoadChunkQueue.Count}\n" +
                  $"Tick:{tickConsumed} ms/t {tickConsumed_μs} μs/t\n" +
                  $"区块分组耗时:{GroupingTime_μs} μs/t";
        _tokenSource = new CancellationTokenSource();
        _processLoadTask = Task.Run(ProcessChunkLoadThread, _tokenSource.Token);
    }

    #region 虚方法和抽象方法

    /// <summary>
    /// 区块时刻，默认实现光照更新和Tick处理
    /// </summary>
    public virtual async void Ticking()
    {
        _stopwatchTick.Restart();
        //方案一
        //单线程区块Tick计算
        // foreach (var chunk in Chunks.Values)
        // {
        //     chunk.Tick(this.World.Service, this.World);
        // }
        //方案二
        //所有相邻的区块组,连续坐标的为一个组
        _stopwatchTick_GroupingTime_.Restart();
        var groups = GetProximityChunkGroup(); //~ 10 μs/t
        _stopwatchTick_GroupingTime_.Stop();
        GroupingTime_μs = _stopwatchTick_GroupingTime_.Elapsed.TotalMicroseconds;

        if (groups.Count > 0)
        {
            Parallel.For(0, groups.Count, (i) =>
            {
                foreach (var pos in groups[i])
                {
                    if (Chunks.TryGetValue(pos, out var chunk))
                        chunk.Tick(World.Service, World);
                }
            });
        }

        //≈ 0 ms
        //方案三
        //使用 Parallel.For 进行并行计算，由于每个区块有50*50大小,读写竞争应该不多
        // var chunks = Chunks.Values.ToArray();
        // Parallel.For(0, chunks.Length,(i)=>
        // {
        //     chunks[i].Tick(World.Service, World);
        // });
        _stopwatchTick.Stop();
        tickConsumed = _stopwatchTick.ElapsedMilliseconds;
        tickConsumed_μs = _stopwatchTick.Elapsed.TotalMicroseconds;

        UpdateLights();
    }

    /// <summary>
    /// 加载并返回区块，这个不能在这个函数内往Chunks添加加载的区块，只能返回,因为这里是个异步方法
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    protected virtual async Task<Chunk> LoadChunk(Vector2I pos)
    {
        try
        {
            using (var conn = SqliteTool.InitSqlite(World.WorldName))
            {
                if (conn.CheckChunkExists(pos.X, pos.Y))
                {
                    var chunk = conn.GetChunkByteData(pos.X, pos.Y);
                    return chunk;
                }
                else
                {
                    var chunk = new Chunk(pos.X, pos.Y);
                    WorldGenerator.Generator(chunk);
                    return chunk;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    /// <summary>
    /// 保存区块，决定区块该不该被保存
    /// </summary>
    /// <param name="chunk"></param>
    public virtual void SaveChunk(Chunk chunk)
    {
        try
        {
            using (var conn = SqliteTool.InitSqlite(World.WorldName))
            {
                if (conn.CheckChunkExists(chunk.X, chunk.Y))
                    conn.UpdateChunkByteData(chunk.X, chunk.Y, chunk);
                else
                    conn.InsertChunkByteValue(chunk.X, chunk.Y, chunk);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// 保存所有
    /// </summary>
    public virtual void SaveAll()
    {
        foreach (var chunkset in Chunks)
        {
            GD.Print($"保存区块:{chunkset.Key.X},{chunkset.Key.Y}");
            SaveChunk(chunkset.Value);
        }
    }

    #endregion

    #region 内部实现

    /// <summary>
    /// 处理区块加载的线程
    /// </summary>
    private async Task ProcessChunkLoadThread()
    {
        while (!_tokenSource.Token.IsCancellationRequested)
        {
            try
            {
                //常规区块加载任务，自动加载玩家半径内的区块
                //异步加载
                var rangeChunks = GetAllLoadRangeChunks();
                var tasks = new List<Task<Chunk>>();
                foreach (var chunkpos in rangeChunks)
                    if (!Chunks.ContainsKey(chunkpos))
                    {
                        //如果和加载队列重叠，先跳过当前的加载。
                        if (LoadChunkQueue.Contains(chunkpos))
                            continue;
                        tasks.Add(LoadChunk(chunkpos));
                    }

                int count = 0;
                //修复死循环问题，
                var queue = LoadChunkQueue.ToArray();
                LoadChunkQueue.Clear();
                for (int i = 0; i < queue.Length; i++)
                {
                    var pos = queue[i];
                    if (Chunks.ContainsKey(pos))
                        continue;
                    tasks.Add(LoadChunk(pos));
                }

                //同步处理
                if (tasks.Count > 0)
                {
                    Chunk[] chunks = await Task.WhenAll(tasks);
                    foreach (var chunk in chunks)
                        if (Chunks.TryAdd(chunk.coord, chunk))
                        {
                            OnChunkLoaded?.Invoke(chunk);
                        }
                }


                //区块卸载,数据去重
                foreach (var chunkpos in Chunks.Keys.ToArray())
                {
                    if (!rangeChunks.Contains(chunkpos))
                    {
                        var chunk = Chunks[chunkpos];
                        //延迟卸载，防止玩家故意卡在两个区块之间
                        if (chunk.RemoveCount++ > 20)
                        {
                            chunk.RemoveCount = 0;
                            OnChunkSaving?.Invoke(chunk);
                            SaveChunk(chunk);
                            Chunks.Remove(chunkpos, out _);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }

            processloadtask_count++;
            await Task.Delay(50, _tokenSource.Token);
        }
    }

    #endregion

    #region 外部工具

    /// <summary>
    /// 获取所有玩家所需加载的区块集合
    /// </summary>
    /// <returns></returns>
    public HashSet<Vector2I> GetAllLoadRangeChunks()
    {
        HashSet<Vector2I> loadqueue = new HashSet<Vector2I>();
        foreach (var player in World.Service.PlayerService.Players.Values)
        {
            if (player.State == PlayerState.Live || player.State == PlayerState.Dead)
                GetLoadRangeChunks(player.ChunkCoord, loadqueue);
        }

        return loadqueue;
    }

    /// <summary>
    /// 获取视距内所需加载的区块坐标
    /// </summary>
    /// <param name="centerCoord">中心坐标</param>
    /// <param name="coords"></param>
    public void GetLoadRangeChunks(Vector2I centerCoord, HashSet<Vector2I> coords)
    {
        for (int X = centerCoord.X - LoadHorizon; X <= centerCoord.X + LoadHorizon; X++)
        {
            for (int Y = centerCoord.Y - LoadHorizon; Y <= centerCoord.Y + LoadHorizon; Y++)
            {
                Vector2I coord = new Vector2I(X, Y);
                if (!coords.Contains(coord))
                    coords.Add(coord);
            }
        }
    }

    /// <summary>
    /// 判断一个方块周围是否被完全包围
    /// </summary>
    /// <param name="pos">坐标</param>
    /// <returns></returns>
    public bool CheckIsCloseBlock(Vector3I pos)
    {
        var block = GetBlock(pos);
        var u = GetBlock(pos + Vector3I.Down);
        var d = GetBlock(pos + Vector3I.Up);
        var l = GetBlock(pos + Vector3I.Left);
        var r = GetBlock(pos + Vector3I.Right);
        if (block != null && u != null && d != null && l != null && r != null)
        {
            if (
                u.BlockMeta.Cube &&
                d.BlockMeta.Cube &&
                l.BlockMeta.Cube &&
                r.BlockMeta.Cube
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试获取全局坐标的方块
    /// </summary>
    /// <param name="globalPosition">全局坐标</param>
    /// <param name="block">方块</param>
    /// <returns>方块是否存在</returns>
    public bool TryGetBlock(Vector3I globalPosition, out BlockData block)
    {
        var b = GetBlock(globalPosition);
        if (b != null)
        {
            block = b;
            return true;
        }

        block = null;
        return false;
    }

    /// <summary>
    /// 获取方块
    /// </summary>
    /// <param name="globalPosition">全局坐标</param>
    /// <returns></returns>
    public BlockData GetBlock(Vector3I globalPosition)
    {
        var coord = globalPosition.MathFloor(Chunk.Size);
        if (Chunks.TryGetValue(coord, out var chunk))
        {
            Vector2I LocalCoord = globalPosition.Remainder(Chunk.Size);
            return chunk.GetBlock(LocalCoord.X, LocalCoord.Y, globalPosition.Z);
        }

        return null;
    }

    /// <summary>
    /// 设置方块
    /// </summary>
    /// <param name="globalPosition">全局坐标</param>
    /// <param name="blockData">方块数据</param>
    /// <returns>返回设置后的方块本身</returns>
    public BlockData SetBlock(Vector3I globalPosition, BlockData blockData)
    {
        var coord = globalPosition.MathFloor(Chunk.Size);
        if (Chunks.TryGetValue(coord, out var chunk))
        {
            Vector2I LocalCoord = globalPosition.Remainder(Chunk.Size);
            return chunk.SetBlock(LocalCoord.X, LocalCoord.Y, globalPosition.Z, blockData);
        }

        return null;
    }

    /// <summary>
    /// 将被动更新方块添加到待更新列表中
    /// </summary>
    /// <param name="globalPosition"></param>
    public void UpdateBlock(Vector3I globalPosition)
    {
        var block = World.Service.ChunkService.GetBlock(globalPosition);
        if (block == null) return;

        var chunkPos = globalPosition.MathFloor(Chunk.Size);
        var localPos = globalPosition.Remainder(Chunk.Size);

        var local = new System.Numerics.Vector3(localPos.X, localPos.Y, globalPosition.Z);
        if (!block.HasComponent<ReactiveComponent>()) return;

        if (World.Service.ChunkService.Chunks.TryGetValue(chunkPos, out var chunk))
            chunk.PassiveTickList.Add(local);
    }

    /// <summary>
    /// 别动更新邻居方块（不包括自身）
    /// </summary>
    /// <param name="globalPosition">全局坐标</param>
    public void PassiveUpdateNeighborBlock(Vector3I globalPosition)
    {
        if (globalPosition.Z == 0)
            UpdateBlock(new Vector3I(globalPosition.X, globalPosition.Y, 1));
        else UpdateBlock(new Vector3I(globalPosition.X, globalPosition.Y, 0));

        UpdateBlock(globalPosition + Vector3I.Up);
        UpdateBlock(globalPosition + Vector3I.Down);
        UpdateBlock(globalPosition + Vector3I.Left);
        UpdateBlock(globalPosition + Vector3I.Right);
    }

    /// <summary>
    /// 设置方块
    /// </summary>
    /// <param name="globalPosition">全局坐标</param>
    /// <param name="meta">方块元数据</param>
    /// <param name="state">方块状态</param>
    /// <returns>设置后的方块</returns>
    public BlockData SetBlock(Vector3I globalPosition, BlockMeta meta, int state = 0)
    {
        var coord = globalPosition.MathFloor(Chunk.Size);
        if (Chunks.TryGetValue(coord, out var chunk))
        {
            Vector2I localPosition = globalPosition.Remainder(Chunk.Size);
            return chunk.SetBlock(localPosition.X, localPosition.Y, globalPosition.Z, meta, state);
        }

        return null;
    }

    /// <summary>
    /// 获取地形集坐标
    /// </summary>
    /// <param name="globalPosition">方块全局坐标</param>
    /// <param name="tagname">匹配标签名</param>
    /// <param name="value">匹配标签值</param>
    /// <returns>Atlas贴图坐标</returns>
    public Vector2I GetTerrain(Vector3I globalPosition, string tagname, string value)
    {
        var block = GetBlock(globalPosition);
        if (block == null) return new Vector2I(1, 1);

        var up = GetBlock(globalPosition + Vector3I.Down);
        var down = GetBlock(globalPosition + Vector3I.Up);
        var left = GetBlock(globalPosition + Vector3I.Left);
        var right = GetBlock(globalPosition + Vector3I.Right);

        int state = 0;
        if (up != null && up.CheckTag(tagname, value)) state |= 1;
        if (down != null && down.CheckTag(tagname, value)) state |= 2;
        if (left != null && left.CheckTag(tagname, value)) state |= 4;
        if (right != null && right.CheckTag(tagname, value)) state |= 8;
        return _terrainCoord[state];
    }

    /// <summary>
    /// 基于光线投射的光照更新
    /// </summary>
    /// <param name="coord">全局坐标</param>
    /// <param name="value">起始光照值</param>
    /// <param name="detail">精细度</param>
    public void RayCastLights(Vector3I coord, int value, int detail = 16)
    {
        var angle_step = detail;
        float angleIncrement = 2 * Mathf.Pi / angle_step;

        for (int angle = 0; angle < angle_step; angle++)
        {
            var currentAngle = angle * angleIncrement;
            var direction = new Godot.Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
            int light = value - 1;
            for (var step = 1; step < value; step++)
            {
                if (light < 0) break;
                var offset = direction * step;
                var CurrentPos = coord + new Vector3I((int)offset.X, (int)offset.Y, 0);
                var block = GetBlock(CurrentPos);
                if (block == null) continue;
                if (block.Light < light)
                    block.Light = light;
                // 遇到完整方块衰减光线
                if (block.BlockMeta.Cube) light -= 2;
                else light -= 1;
            }
        }
    }

    /// <summary>
    /// 基于DFS的光照更新
    /// </summary>
    /// <param name="coord">全局坐标</param>
    /// <param name="value">起始光照值</param>
    public void DfsUpdateLight(Vector3I coord, int value)
    {
        if (value <= 0) return;

        var block = GetBlock(coord);
        if (block == null) return;
        if (block.Light < value)
        {
            block.Light = value;
        }
        else
        {
            return;
        }

        if (block.BlockMeta.Cube) value -= 2;
        else value -= 1;

        DfsUpdateLight(coord - Vector3I.Left, value);
        DfsUpdateLight(coord - Vector3I.Right, value);
        DfsUpdateLight(coord - Vector3I.Up, value);
        DfsUpdateLight(coord - Vector3I.Down, value);
    }

    /// <summary>
    /// 更新单个区块的光源
    /// </summary>
    /// <param name="chunk">区块</param>
    public void UpdataChunkLight(Chunk chunk)
    {
        if (LightMode == LightModeEnum.None)
        {
            return;
        }

        var highmap = chunk.HighMap;
        if (highmap == null)
        {
            GD.PrintErr("highmap is null!");
            //chunk.HighMap = WorldGenerator.GetHighMap(chunk.X);
        }

        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                var gx = chunk.X * Chunk.Size + x;
                var gy = chunk.Y * Chunk.Size + y;
                var num = highmap[x, 1] - gy;
                if (num > 0) chunk.GetBlock(x, y, 1).SetLight(SkyLight);
                if (num == 0)
                {
                    if (LightMode == LightModeEnum.RayCastMode) RayCastLights(new Vector3I(gx, gy, 1), LightSize);
                    if (LightMode == LightModeEnum.DFSMode) DfsUpdateLight(new Vector3I(gx, gy, 1), LightSize);
                }
            }
        }

        if (chunk.LightList.Count > 0)
        {
            foreach (var point in chunk.LightList)
            {
                var light = new Vector2I(chunk.X * Chunk.Size + (int)point.X,
                    chunk.Y * Chunk.Size + (int)point.Y);
                if (LightMode == LightModeEnum.DFSMode)
                    DfsUpdateLight(new Vector3I((int)light.X, (int)light.Y, 1), LightSize);
                if (LightMode == LightModeEnum.RayCastMode)
                    RayCastLights(new Vector3I((int)light.X, (int)light.Y, 1), LightSize);
            }
        }
    }

    /// <summary>
    /// 只更新主控玩家加载范围内的光照
    /// </summary>
    public void UpdateLights()
    {
        var player = World.PlayerNode.playerData;
        if (player == null) return;
        var poss = new HashSet<Vector2I>();
        GetLoadRangeChunks(player.ChunkCoord, poss);
        int light = 0;
        if (LightMode == LightModeEnum.None) light = 16;
        List<Chunk> resultchunk = new List<Chunk>();
        foreach (var sts in Chunks)
        {
            var chunk = sts.Value;
            if (poss.Contains(chunk.coord))
            {
                resultchunk.Add(sts.Value);
                sts.Value.SetLight(light);
            }
        }

        if (LightMode != LightModeEnum.None)
        {
            foreach (var sts in Chunks)
            {
                var chunk = sts.Value;
                if (poss.Contains(chunk.coord))
                    UpdataChunkLight(chunk);
            }

            int lightsize = LightSize / 2;
            if (player.Mode == 1) lightsize = LightSize * 4;
            if (LightMode == LightModeEnum.DFSMode)
                DfsUpdateLight(new Vector3I(player.Coord.X, player.Coord.Y, 1), lightsize);
            if (LightMode == LightModeEnum.RayCastMode)
                RayCastLights(new Vector3I(player.Coord.X, player.Coord.Y, 1), lightsize, 32);
        }

        foreach (var chunk in resultchunk)
        {
            chunk.CheckLightUpdate();
        }
    }

    /// <summary>
    /// 获取包含指定组件的邻居方块。
    /// </summary>
    /// <param name="pos"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<BlockData> GetBlockAsSameComponent<T>(Vector3I pos) where T : Component
    {
        List<BlockData> result = new List<BlockData>();
        //前后
        {
            BlockData block;
            if (pos.Z == 0)
                block = GetBlock(new Vector3I(pos.X, pos.Y, 1));
            else block = GetBlock(new Vector3I(pos.X, pos.Y, 0));
            if (block?.GetComponent<T>() != null)
            {
                result.Add(block);
            }
        }

        {
            BlockData block = GetBlock(pos - Vector3I.Left);
            if (block?.GetComponent<T>() != null)
                result.Add(block);
        }
        {
            BlockData block = GetBlock(pos - Vector3I.Right);
            if (block?.GetComponent<T>() != null)
                result.Add(block);
        }
        {
            BlockData block = GetBlock(pos - Vector3I.Up);
            if (block?.GetComponent<T>() != null)
                result.Add(block);
        }
        {
            BlockData block = GetBlock(pos - Vector3I.Down);
            if (block?.GetComponent<T>() != null)
                result.Add(block);
        }

        return result;
    }

    /// <summary>
    /// 连续区块分组
    /// </summary>
    /// <returns></returns>
    public List<List<Vector2I>> GetProximityChunkGroup()
    {
        List<List<Vector2I>> result = new List<List<Vector2I>>();
        HashSet<Vector2I> enterys = new();
        foreach (var pos in Chunks.Keys.ToArray())
        {
            if (!enterys.Contains(pos))
            {
                List<Vector2I> group = new List<Vector2I>();
                GetProximityChunk(group, pos.X, pos.Y);
                result.Add(group);
            }
        }

        return result;

        void GetProximityChunk(List<Vector2I> group, int x, int y)
        {
            var pos = new Vector2I(x, y);
            if (!enterys.Contains(pos) && Chunks.ContainsKey(pos))
            {
                enterys.Add(pos);
                group.Add(pos);
                GetProximityChunk(group, x + 1, y);
                GetProximityChunk(group, x - 1, y);
                GetProximityChunk(group, x, y + 1);
                GetProximityChunk(group, x, y - 1);
            }
        }
    }

    #endregion

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _tokenSource.Cancel();
        _processLoadTask?.Wait(1000);
        _processLoadTask?.Dispose();
    }
}