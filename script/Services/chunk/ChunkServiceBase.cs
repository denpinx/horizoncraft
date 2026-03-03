using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Expand;
using Horizoncraft.script.WorldControl;
using Horizoncraft.script.WorldControl.Tool;


namespace Horizoncraft.script.Services.chunk;

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
    /// 每帧调用的主逻辑入口，负责：
    /// <list type="bullet">
    ///   <item>对已加载区块进行 Tick 更新（按连续区块分组并行处理）</item>
    ///   <item>根据当前光照模式更新光照</item>
    /// </list>
    /// 此方法由世界定时器驱动，默认每帧执行一次。
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
    /// 异步加载指定坐标的区块。
    /// <para>优先从 SQLite 数据库读取；若不存在，则通过世界生成器创建新区块。</para>
    /// </summary>
    /// <param name="pos">区块坐标（以 Chunk.Size 为单位）</param>
    /// <returns>加载完成的 <see cref="Chunk"/> 实例，失败时返回 <c>null</c></returns>
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
    /// 将指定区块保存到 SQLite 数据库。
    /// <para>若数据库中已存在该区块坐标，则更新；否则插入新记录。</para>
    /// </summary>
    /// <param name="chunk">待保存的区块实例</param>
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
    /// 保存所有当前内存中的已加载区块到磁盘。
    /// <para>通常在游戏退出或手动存档时调用。</para>
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
    /// 确保以 <paramref name="coord"/> 为中心、半径为 <paramref name="range"/> 的区域内所有区块都处于加载状态。
    /// <para>未加载的区块会被加入加载队列（<see cref="LoadChunkQueue"/>），但不会立即加载。</para>
    /// </summary>
    /// <param name="coord">中心区块坐标</param>
    /// <param name="range">检查半径（单位：区块数）</param>
    /// <returns>若区域内所有区块均已加载，返回 <c>true</c>；否则返回 <c>false</c> 并触发异步加载。</returns>
    public bool EnsureChunksLoaded(Vector2I coord, int range)
    {
        bool exist = true;
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                var pos = coord + new Vector2I(x, y);
                if (!Chunks.ContainsKey(pos))
                {
                    exist = false;
                    if (!LoadChunkQueue.Contains(pos))
                        LoadChunkQueue.Enqueue(pos);
                }
            }
        }

        if (exist)
            return true;
        return false;
    }


    /// <summary>
    /// 获取所有活跃玩家（存活或死亡状态）视距范围内需要加载的区块坐标集合。
    /// <para>用于确定哪些区块应保留在内存中。</para>
    /// </summary>
    /// <returns>需加载的区块坐标集合（去重）</returns>
    public HashSet<Vector2I> GetAllLoadRangeChunks()
    {
        HashSet<Vector2I> loadqueue = new HashSet<Vector2I>();
        if (World?.Service?.PlayerService?.Players?.Values != null)
            foreach (var player in World.Service.PlayerService.Players.Values)
            {
                if (player.State == PlayerState.Live || player.State == PlayerState.Dead)
                    GetLoadRangeChunks(player.ChunkCoord, loadqueue);
            }
        else
        {
            // this._tokenSource.Cancel();
        }

        return loadqueue;
    }

    /// <summary>
    /// 将以 <paramref name="centerCoord"/> 为中心、半径为 <see cref="LoadHorizon"/> 的区块坐标添加到 <paramref name="coords"/> 集合中。
    /// </summary>
    /// <param name="centerCoord">中心区块坐标</param>
    /// <param name="coords">目标集合，用于累积结果（避免重复分配）</param>
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
    /// 判断指定全局坐标的方块是否被上下左右四个方向的“立方体”方块完全包围。
    /// <para>用于优化渲染或物理计算（如气体扩散、声音传播等）。</para>
    /// </summary>
    /// <param name="pos">全局三维坐标（X, Y, Z）</param>
    /// <returns>若被完全包围且自身及邻居均为有效立方体方块，返回 <c>true</c>；否则返回 <c>false</c></returns>
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
    /// 尝试获取指定全局坐标的方块数据。
    /// </summary>
    /// <param name="globalPosition">全局三维坐标</param>
    /// <param name="block">输出参数：若存在则返回方块数据，否则为 <c>null</c></param>
    /// <returns>若方块存在且所在区块已加载，返回 <c>true</c>；否则返回 <c>false</c></returns>
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
    /// 获取指定全局坐标的方块数据。
    /// <para>若所在区块未加载，返回 <c>null</c>。</para>
    /// </summary>
    /// <param name="globalPosition">全局三维坐标（X, Y, Z）</param>
    /// <returns>方块数据，或 <c>null</c>（若区块未加载或坐标越界）</returns>
    public BlockData GetBlock(Vector3I globalPosition)
    {
        // todo: 这里可以用四叉树去优化，得改区块结构
        var coord = globalPosition.MathFloor(Chunk.Size);
        if (Chunks.TryGetValue(coord, out var chunk))
        {
            Vector2I LocalCoord = globalPosition.Remainder(Chunk.Size);
            return chunk.GetBlock(LocalCoord.X, LocalCoord.Y, globalPosition.Z);
        }

        return null;
    }

    /// <summary>
    /// 在指定全局坐标设置方块数据。
    /// <para>仅当所在区块已加载时生效。</para>
    /// </summary>
    /// <param name="globalPosition">全局三维坐标</param>
    /// <param name="blockData">要设置的方块数据</param>
    /// <returns>设置后的方块实例，若失败（如区块未加载）则返回 <c>null</c></returns>
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
    /// 将指定坐标的方块加入其所在区块的被动更新列表（<see cref="Chunk.PassiveTickList"/>）。
    /// <para>用于触发响应式组件（如电路、植物生长）的后续处理。</para>
    /// </summary>
    /// <param name="globalPosition">全局坐标</param>
    /// <param name="deep">递归深度，用于同时标记邻居方块（默认为 0，不递归）</param>
    public void UpdateBlock(Vector3I globalPosition, int deep = 0)
    {
        var block = World.Service.ChunkService.GetBlock(globalPosition);
        if (block == null) return;

        var chunkPos = globalPosition.MathFloor(Chunk.Size);
        var localPos = globalPosition.Remainder(Chunk.Size);

        var local = new System.Numerics.Vector3(localPos.X, localPos.Y, globalPosition.Z);
        //if (!block.HasComponent<ReactiveComponent>()) return;

        if (World.Service.ChunkService.Chunks.TryGetValue(chunkPos, out var chunk))
            chunk.PassiveTickList.Add(local);
        if (deep > 0)
        {
            PassiveUpdateNeighborBlock(globalPosition, true, deep - 1);
        }
    }

    /// <summary>
    /// 触发指定方块及其邻居的被动更新。
    /// <para>常用于方块状态变化后通知周围环境（如放置火把点亮周围）。</para>
    /// </summary>
    /// <param name="globalPosition">中心坐标</param>
    /// <param name="inside">是否包含自身更新</param>
    /// <param name="deep">递归深度（控制影响范围）</param>
    public void PassiveUpdateNeighborBlock(Vector3I globalPosition, bool inside = false, int deep = 0)
    {
        if (inside)
            UpdateBlock(globalPosition);
        if (globalPosition.Z == 0)
            UpdateBlock(new Vector3I(globalPosition.X, globalPosition.Y, 1));
        else UpdateBlock(new Vector3I(globalPosition.X, globalPosition.Y, 0));

        UpdateBlock(globalPosition + Vector3I.Up, deep - 1);
        UpdateBlock(globalPosition + Vector3I.Down, deep - 1);
        UpdateBlock(globalPosition + Vector3I.Left, deep - 1);
        UpdateBlock(globalPosition + Vector3I.Right, deep - 1);
    }

    /// <summary>
    /// 使用元数据和状态值在指定全局坐标创建并设置方块。
    /// </summary>
    /// <param name="globalPosition">全局三维坐标</param>
    /// <param name="meta">方块元数据</param>
    /// <param name="state">方块状态（默认为 0）</param>
    /// <returns>设置后的方块实例，若失败则返回 <c>null</c></returns>
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
    /// 根据相邻方块的标签匹配情况，计算当前方块在 Atlas 贴图中的 UV 坐标（用于自动拼接纹理）。
    /// <para>使用预定义的 <see cref="_terrainCoord"/> 映射表。</para>
    /// </summary>
    /// <param name="globalPosition">当前方块的全局坐标</param>
    /// <param name="tagname">要匹配的标签名称（如 "terrain"）</param>
    /// <param name="value">期望的标签值（如 "grass"）</param>
    /// <returns>Atlas 贴图中的坐标（Vector2I），默认为 (1,1) 表示“全包围”</returns>
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
    /// 使用光线投射方式从指定点向外扩散光照。
    /// <para>模拟圆形光照衰减，适用于火把、灯等点光源。</para>
    /// </summary>
    /// <param name="coord">光源起始全局坐标（Z=1）</param>
    /// <param name="value">初始光照强度（最大值）</param>
    /// <param name="detail">光线数量（角度采样精度，默认 16 条射线）</param>
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
    /// 使用深度优先搜索（DFS）递归更新光照。
    /// <para>性能较高但可能栈溢出，适用于小范围或可控光源。</para>
    /// </summary>
    /// <param name="coord">起始坐标</param>
    /// <param name="value">当前光照强度</param>
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
    /// 更新指定区块内的光照信息，包括：
    /// <list type="bullet">
    ///   <item>天空光照（基于高度图）</item>
    ///   <item>区块内静态光源（<see cref="Chunk.LightList"/>）</item>
    /// </list>
    /// </summary>
    /// <param name="chunk">要更新的区块</param>
    public void UpdateChunkLight(Chunk chunk)
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
    /// 更新主控玩家视距范围内的所有光照。
    /// <para>包括天空光、区块光源以及玩家手持光源（如夜视模式增强）。</para>
    /// </summary>
    public void UpdateLights()
    {
        if (World.PlayerNode == null) return;
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
                    UpdateChunkLight(chunk);
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
    /// 获取指定方块的上下左右及前后邻居中，带有指定组件 <typeparamref name="T"/> 的方块列表。
    /// </summary>
    /// <typeparam name="T">要查找的组件类型（需继承自 <see cref="Component"/>）</typeparam>
    /// <param name="pos">中心方块的全局坐标</param>
    /// <returns>符合条件的邻居方块列表</returns>
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
    /// 将当前所有已加载区块按“四连通连续区域”分组。
    /// <para>用于并行 Tick 时减少线程竞争（同一组内区块相邻，可串行处理）。</para>
    /// </summary>
    /// <returns>区块坐标分组列表，每组为一个连续区域</returns>
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