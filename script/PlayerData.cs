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
    //连接id
    public int PeerId;

    public ConfigSet<float> Resistance = new() { Value = 1f, Default = 1f };
    public ConfigSet<float> MoveSpeed = new() { Value = 16 * 5f, Default = 16 * 5f };
    public ConfigSet<bool> Fly = new() { Value = false, Default = false };

    //玩家名
    public String Name;

    //位置
    public Vector2 Position;
    //面朝方向
    public bool FaceLeft = false;
    //是否打开容器，这里相当于是是否订阅
    public bool OpeningBlockInventory = false;

    //当前打开容器的坐标
    public Vector3 OpenInventory;

    //物品栏
    public PlayerInventory Inventory = new();

    //
    public int Mode = 0;
    

    [MemoryPackIgnore] public Player player;

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