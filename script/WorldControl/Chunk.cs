using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using MemoryPack;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Events;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Services.world;
using ReactiveComponent = Horizoncraft.script.Components.BlockComponents.ReactiveComponent;
using TickComponent = Horizoncraft.script.Components.BlockComponents.TickComponent;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Horizoncraft.script.WorldControl;

[MemoryPackable]
public partial class Chunk
{
    /// <summary>
    /// 区块的层数
    /// </summary>
    public static int SizeZ = 2;

    /// <summary>
    /// 区块大小
    /// </summary>
    public static int Size = 50;

    /// <summary>
    /// 每Tick随机触发Tick事件数量
    /// </summary>
    public static int RandTickPerCount = Size / 4;

    /// <summary>
    /// 世界是否生成
    /// </summary>
    public bool Spawn = false;

    /// <summary>
    /// 区块版本
    /// </summary>
    public long Version;

    /// <summary>
    /// 区块X轴坐标
    /// </summary>
    public int X;

    /// <summary>
    /// 区块Y轴坐标
    /// </summary>
    public int Y;

    /// <summary>
    /// 世界生成时的高度图
    /// </summary>
    public int[,] HighMap = new int[Size, SizeZ];

    /// <summary>
    /// 生物群系类型
    /// </summary>
    public string BiomeType = "";

    /// <summary>
    /// 区块卸载计数
    /// 累计到达20，就触发卸载
    /// </summary>
    public int UnloadCount = 0;

    /// <summary>
    /// 主动触发Tick列表
    /// </summary>
    public HashSet<Vector3> TickList = new();

    /// <summary>
    /// 被动触发Tick列表
    /// </summary>
    public HashSet<Vector3> PassiveTickList = new();

    [MemoryPackIgnore] private readonly List<Vector3> _passiveTickBuffer = new(64);
    [MemoryPackIgnore] private readonly List<Vector3> _tickBuffer = new(64);
    [MemoryPackIgnore] private readonly HashSet<Vector3I> _randPosBuffer = new(RandTickPerCount);
    [MemoryPackIgnore] private readonly BlockTickEvent _blockTickEvent = new();
    [MemoryPackIgnore] private readonly BlockTickEvent _randTickEvent = new();

    /// <summary>
    /// 光照更新列表
    /// </summary>
    public HashSet<Vector2> LightList = new();

    /// <summary>
    /// 光照更新次数计数
    /// </summary>
    [MemoryPackIgnore] public int LightUpdateTime = 0;

    /// <summary>
    /// 方块变更列表，用于差异更新统计
    /// </summary>
    [MemoryPackIgnore] public List<Vector3I> UpdateList = new(32);

    /// <summary>
    /// 方块变更列表缓存
    /// </summary>
    [MemoryPackIgnore] public List<Vector3I> UpdateList_buffer = new();

    /// <summary>
    /// 设置为true，会使区块tilemap全量更新方块
    /// </summary>
    [MemoryPackIgnore] public bool TileMapFullUpdate = true;

    /// <summary>
    /// 设置为true，会使区块被全量更新给附近的玩家。
    /// </summary>
    [MemoryPackIgnore] public bool ServerFullUpdate = true;

    /// <summary>
    /// 区块Tick更新耗时,单位：μs
    /// </summary>
    [MemoryPackIgnore] public double TickUsedTimeμs;

    /// <summary>
    /// 计时器实列
    /// </summary>
    [MemoryPackIgnore] public Stopwatch _Stopwatch_tick_used = new Stopwatch();

    [MemoryPackIgnore]
    public Godot.Vector2I coord
    {
        get { return new(X, Y); }
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    /// 加载完区块后释放到世界中，卸载区块时再从世界中捕获
    /// </summary>
    public List<EntityData> Entitys = new();

    /// <summary>
    /// 方块数组
    /// </summary>
    public BlockData[,,] data = new BlockData[Size, Size, SizeZ];

    /// <summary>
    /// 区块生成耗时,单位：μs
    /// </summary>
    public double SpawnCostTime_μs;

    [MemoryPackConstructor]
    public Chunk()
    {
    }

    public Chunk(int x, int y)
    {
        this.X = x;
        this.Y = y;
        for (int X = 0; X < Chunk.Size; X++)
        {
            for (int Y = 0; Y < Chunk.Size; Y++)
            {
                for (int Z = 0; Z < Chunk.SizeZ; Z++)
                {
                    data[X, Y, Z] = Materials.Valueof("air").CreateBlockData();
                }
            }
        }
    }

    /// <summary>
    /// 获取方块
    /// 使用局部坐标
    /// </summary>
    /// <param name="x">0到区块的最大尺寸之内</param>
    /// <param name="y">0到区块的最大尺寸之内</param>
    /// <param name="z">0到区块的最大层数之内</param>
    /// <returns>方块实列</returns>
    public BlockData GetBlock(int x, int y, int z)
    {
        return data[x, y, z];
    }

    /// <summary>
    /// 设置方块元数据
    /// 使用局部坐标
    /// </summary>
    /// <param name="x">0到区块的最大尺寸之内</param>
    /// <param name="y">0到区块的最大尺寸之内</param>
    /// <param name="z">0到区块的最大层数之内</param>
    /// <param name="meta">元数据</param>
    /// <param name="state">方块状态</param>
    /// <returns>方块实列</returns>
    public BlockData SetBlock(int x, int y, int z, BlockMeta meta, int state = 0)
    {
        var pos = new Vector3(x, y, z);
        var posv2 = new Vector2(x, y);
        if (meta.HasComponent<TickComponent>())
        {
            TickList.Add(pos);
        }
        else TickList.Remove(pos);

        if (meta.Light)
        {
            if (!LightList.Contains(posv2))
            {
                LightList.Add(posv2);
            }
        }
        else if (data[x, y, z].BlockMeta.Light)
        {
            LightList.Remove(posv2);
        }


        data[x, y, z].SetMeta(meta);
        data[x, y, z].State = state;
        UpdateList_buffer.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z));
        TileMapFullUpdate = true;
        return data[x, y, z];
    }

    /// <summary>
    /// 设置区块内的方块为指定方块实列
    /// 使用局部坐标
    /// </summary>
    /// <param name="x">0到区块的最大尺寸之内</param>
    /// <param name="y">0到区块的最大尺寸之内</param>
    /// <param name="z">0到区块的最大层数之内</param>
    /// <param name="blockData">方块实列</param>
    /// <param name="state">状态</param>
    /// <returns></returns>
    public BlockData SetBlock(int x, int y, int z, BlockData blockData, int state = 0)
    {
        var pos = new Vector3(x, y, z);
        var posv2 = new Vector2(x, y);
        if (blockData.GetComponent<TickComponent>() != null)
        {
            TickList.Add(pos);
        }
        else TickList.Remove(pos);

        if (blockData.BlockMeta.Light)
        {
            if (!LightList.Contains(posv2))
            {
                LightList.Add(posv2);
            }
        }
        else if (data[x, y, z].BlockMeta.Light)
        {
            LightList.Remove(posv2);
        }

        data[x, y, z].Components.Clear();
        var l = data[x, y, z].Light;
        data[x, y, z] = blockData;
        data[x, y, z].State = state;
        data[x, y, z].Light = l;
        UpdateList_buffer.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z));
        TileMapFullUpdate = true;
        return data[x, y, z];
    }

    /// <summary>
    /// 触发当前区块的随机Tick更新
    /// </summary>
    /// <param name="worldService">世界服务</param>
    /// <param name="world">世界实列</param>
    public void TriggerRandTick(WorldServiceBase worldService, World world)
    {
        _randPosBuffer.Clear();
        for (int i = 0; i < RandTickPerCount;)
        {
            var pos = new Vector3I(Random.Shared.Next(0, Size), Random.Shared.Next(0, Size),
                Random.Shared.Next(0, SizeZ));
            if (_randPosBuffer.Add(pos))
            {
                i++;
            }
        }

        _randTickEvent.World = world;
        _randTickEvent.Service = worldService;
        _randTickEvent.Chunk = this;
        foreach (var pos in _randPosBuffer)
        {
            _randTickEvent.LocalPos = pos;
            _randTickEvent.GlobalePos = new Vector3I(
                coord.X * Size + pos.X
                , coord.Y * Size + pos.Y
                , pos.Z);
            var block = GetBlock(pos.X, pos.Y, pos.Z);
            _randTickEvent.BlockData = block;
            if (block?.Components == null) continue;

            var stateStart = block.State;

            worldService.NeoComponentManager.ExecuteRandBlockComponents(_randTickEvent, block);

            if (stateStart != block.State)
            {
                TileMapFullUpdate = true;
                UpdateList_buffer.Add(_randTickEvent.LocalPos);
            }

            _randTickEvent.Reset();
        }
    }

    /// <summary>
    /// 触发当前区块的Tick更新
    /// 50*50*2 2500个tick对象的情况下，平均每个区块最大耗时 1ms
    /// </summary>
    /// <param name="worldService">世界服务</param>
    /// <param name="world">世界实列</param>
    public void TriggerTick(WorldServiceBase worldService, World world)
    {
        _Stopwatch_tick_used.Restart();
        Version = worldService.TickTimes;

        (UpdateList, UpdateList_buffer) = (UpdateList_buffer, UpdateList);
        UpdateList.Clear();


        _blockTickEvent.World = world;
        _blockTickEvent.Service = worldService;
        _blockTickEvent.Chunk = this;
        _passiveTickBuffer.Clear();
        _passiveTickBuffer.AddRange(PassiveTickList);
        PassiveTickList.Clear();

        //TODO 同时拥有被动更新和主动更新会导致主动更新被更新两次

        foreach (var pos in _passiveTickBuffer)
        {
            var block = GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
            if (block?.Components == null) continue;
            bool exe_fail = false;
            foreach (var cmp in block.Components)
            {
                var globale = new Vector3I((int)(this.coord.X * Chunk.Size + pos.X)
                    , (int)(this.coord.Y * Chunk.Size + pos.Y)
                    , (int)pos.Z);
                if (cmp is ReactiveComponent)
                {
                    _blockTickEvent.BlockData = block;
                    _blockTickEvent.GlobalePos = globale;
                    _blockTickEvent.LocalPos = pos.ToVector3I();
                    var state_start = block.State;
                    _blockTickEvent.Reset();

                    if (!worldService.NeoComponentManager.ExecuteBlockComponents(_blockTickEvent, block))
                    {
                        exe_fail = true;
                        break;
                    }

                    if (state_start != block.State)
                    {
                        TileMapFullUpdate = true;
                        UpdateList_buffer.Add(_blockTickEvent.LocalPos);
                    }
                }
            }

            if (exe_fail)
            {
                TileMapFullUpdate = true;
                UpdateList_buffer.Add(_blockTickEvent.LocalPos);
            }
        }


        var coord = new Vector3I(0, 0, 0);
        var local = new Vector3I(0, 0, 0);
        string id;
        int state;
        _tickBuffer.Clear();
        _tickBuffer.AddRange(TickList);
        foreach (var item in _tickBuffer)
        {
            local.X = (int)item.X;
            local.Y = (int)item.Y;
            local.Z = (int)item.Z;
            coord.X = this.coord.X * Chunk.Size + (int)item.X;
            coord.Y = this.coord.Y * Chunk.Size + (int)item.Y;
            coord.Z = (int)item.Z;
            var block = GetBlock(local.X, local.Y, local.Z);
            if (block.Components.Count != 0)
            {
                _blockTickEvent.BlockData = block;
                _blockTickEvent.GlobalePos = coord;
                _blockTickEvent.LocalPos = local;
                id = block.Id;
                state = block.State;
                try
                {
                    worldService.NeoComponentManager.ExecuteBlockComponents(_blockTickEvent, block);
                    // ComponentManager.ExecuteBlockComponents(_blockTickEvent, block);
                    _blockTickEvent.Reset();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                if (state != block.State || id != block.Id)
                {
                    TileMapFullUpdate = true;
                    UpdateList.Add(local);
                }
            }
            else //异常方块重置组件
            if (block.BlockMeta.Components.Count != 0)
            {
                block.SetMeta(block.BlockMeta);
                TileMapFullUpdate = true;
                UpdateList.Add(local);
            }
        }

        TriggerRandTick(worldService, world);

        _Stopwatch_tick_used.Stop();
        TickUsedTimeμs = _Stopwatch_tick_used.Elapsed.TotalMicroseconds;
    }

    /// <summary>
    /// 填充区块的光照值
    /// </summary>
    /// <param name="light">光照值</param>
    public void FillLight(int light)
    {
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        {
            data[x, y, 1].OldLight = data[x, y, 1].Light;
            data[x, y, 1].Light = light;
        }
    }

    /// <summary>
    /// 检查光照是否有变动
    /// </summary>
    /// <returns>true:发生变动，false:无变更</returns>
    public bool CheckLightUpdate()
    {
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        {
            if (data[x, y, 1].OldLight != data[x, y, 1].Light)
            {
                LightUpdateTime++;
                TileMapFullUpdate = true;
                return true;
            }
        }

        return false;
    }
}