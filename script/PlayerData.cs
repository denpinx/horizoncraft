using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;
using Vector2I = Godot.Vector2I;
using Vector3 = System.Numerics.Vector3;

namespace horizoncraft.script;

[MemoryPackable]
public partial class PlayerData
{
    [MemoryPackAllowSerialize] private Vector2 _position;
    [MemoryPackAllowSerialize] private bool _faceLeft;
    [MemoryPackAllowSerialize] private bool _openingBlockInventory;

    [MemoryPackAllowSerialize] private int _mode;

    //连接id
    public int PeerId = 0;

    public ConfigSet<float> Resistance = new() { Value = 1f, Default = 1f };
    public ConfigSet<float> MoveSpeed = new() { Value = 16 * 5f, Default = 16 * 5f };
    public ConfigSet<bool> Fly = new() { Value = false, Default = false };


    public int RemoveCount = 0;

    public bool Update = false;

    //玩家名
    public String Name;

    //位置
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

    //面朝方向
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

    //是否打开容器，这里相当于是是否订阅
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

    //当前打开容器的坐标
    public Vector3 OpenInventory;

    //物品栏
    public PlayerInventory Inventory = new();

    //
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


    [MemoryPackIgnore] public PlayerNode PlayerNode;

    [MemoryPackIgnore]
    public Vector2I Coord
    {
        get { return World.MathFloor(new Vector2I((int)Position.X, (int)Position.Y), 16); }
    }

    [MemoryPackIgnore]
    public Vector2I Position_v2i
    {
        get { return new Vector2I((int)Position.X, (int)Position.Y); }
    }

    [MemoryPackIgnore]
    public Godot.Vector2 Position_v2
    {
        get { return new Godot.Vector2((int)Position.X, (int)Position.Y); }
    }

    [MemoryPackIgnore]
    public Vector2I ChunkCoord
    {
        get { return World.MathFloor(new Vector2I((int)Position.X, (int)Position.Y), Chunk.Size * 16); }
    }

    public PlayerData()
    {
    }
}