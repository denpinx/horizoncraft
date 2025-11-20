using System;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Net;
using Horizoncraft.script.WorldControl;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;
using Vector2I = Godot.Vector2I;
using Vector3 = System.Numerics.Vector3;

namespace Horizoncraft.script;

[MemoryPackable]
public partial class PlayerData
{
    //已经加载实体的所有uuid
    [MemoryPackIgnore] public EntityUuidPack EntityUuidPack = new EntityUuidPack();
    [MemoryPackIgnore] public EntityUuidPack LastFarmeEntityUuidPack = new EntityUuidPack();
    [MemoryPackIgnore] public Vector2 LastPosition = Vector2.Zero;

    [MemoryPackAllowSerialize] private Vector2 _position;
    [MemoryPackAllowSerialize] private bool _faceLeft;
    [MemoryPackAllowSerialize] private bool _openingBlockInventory;
    [MemoryPackAllowSerialize] private int _mode;
    [MemoryPackAllowSerialize] private PlayerState _state = PlayerState.Respawning;

    //连接id
    public int PeerId = 0;

    public PlayerState State
    {
        get { return _state; }
        set
        {
            _state = value;
            if (value == PlayerState.Respawning && !HasSpawnPoint)
                SpawnPoint = GetFuzzySpawnPoint();
            Update = true;
        }
    }

    public string Deathrattle = "";

    public bool HasSpawnPoint = false;
    public Vector2 SpawnPoint;

    /// <summary>
    /// 生命值
    /// </summary>
    public ConfigSet<float> Health = new() { Value = 20f, Default = 20f };

    /// <summary>
    /// 饥饿值
    /// </summary>
    public ConfigSet<float> Hunger = new() { Value = 20f, Default = 20f };

    /// <summary>
    /// 阻力
    /// </summary>
    public ConfigSet<float> Resistance = new() { Value = 1f, Default = 1f };

    /// <summary>
    /// 移动速度
    /// </summary>
    public ConfigSet<float> MoveSpeed = new() { Value = 16 * 5f, Default = 16 * 5f };

    /// <summary>
    /// 是否处于飞行
    /// </summary>
    public ConfigSet<bool> Fly = new() { Value = false, Default = false };

    /// <summary>
    /// 卸载计时器
    /// </summary>
    public int RemoveCount = 0;

    /// <summary>
    /// 数据是否有更新
    /// </summary>
    public bool Update = false;

    /// <summary>
    /// 玩家名
    /// </summary>
    public String Name;

    /// <summary>
    /// 玩家坐标
    /// </summary>
    public Vector2 Position
    {
        get => _position;
        set
        {
            if (_position != value)
                Update = true;
            _position = value;
        }
    }

    /// <summary>
    /// 面朝方向
    /// </summary>
    public bool FaceLeft
    {
        get => _faceLeft;
        set
        {
            if (_faceLeft != value)
                Update = true;
            _faceLeft = value;
        }
    }

    /// <summary>
    ///是否打开容器，这里相当于是否订阅方块的组件更新
    /// </summary>
    public bool OpeningBlockInventory
    {
        get => _openingBlockInventory;
        set
        {
            if (_openingBlockInventory != value)
                Update = true;
            _openingBlockInventory = value;
        }
    }

    /// <summary>
    /// 当前打开的容器坐标
    /// </summary>
    public Vector3 OpenInventory;

    /// <summary>
    /// 物品栏
    /// </summary>
    public PlayerInventory Inventory = new();

    /// <summary>
    /// 玩家模式
    /// </summary>
    public int Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
                Update = true;
            _mode = value;
        }
    }

    /// <summary>
    /// 玩家所在的方块全局坐标
    /// </summary>
    [MemoryPackIgnore]
    public Vector2I Coord => _position.MathFloor(16);

    [MemoryPackIgnore] public Vector2I Position_v2i => _position.ToVector2I();
    [MemoryPackIgnore] public Godot.Vector2 Position_v2 => _position.ToGodotVector2();

    /// <summary>
    ///玩家所在的区块全局坐标
    /// </summary>
    [MemoryPackIgnore]
    public Vector2I ChunkCoord => _position.MathFloor(Chunk.Size * 16);

    public PlayerData()
    {
    }

    /// <summary>
    /// 搜寻模糊随机复活点
    /// </summary>
    /// <param name="player">玩家数据</param>
    public Vector2 GetFuzzySpawnPoint()
    {
        int ChunkX = Random.Shared.Next(-16, 16);
        var map = WorldGenerator.GetHighMap(ChunkX);
        int randx = Random.Shared.Next(0, Chunk.Size);
        int gy = map[randx, 1];

        return new Vector2(ChunkX * Chunk.Size + randx, gy);
    }
}

public enum Cancel
{
    None,
    PlaceBlock,
    BreakBlock,
    OpenBlock,
    UseBlock,
}