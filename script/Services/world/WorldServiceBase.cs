using System;
using System.Globalization;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script;
using horizoncraft.script.Entity;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.rpc;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using HorizonCraft.script.Services.player;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.world;

/// <summary>
/// 世界服务基础
/// </summary>
public abstract class WorldServiceBase : IDisposable
{
    const int DayTicks = 20 * 60;
    public WorldProfile Profile;
    public int TickTimes;
    public World World;

    /// <summary>区块服务</summary>
    public ChunkServiceBase ChunkService;

    public ChunkServiceNode ChunkServiceNode;

    /// <summary>玩家服务</summary>
    public PlayerServiceBase PlayerService;

    public PlayerServiceNode PlayerServiceNode;

    /// <summary>实体服务</summary>
    public EntityServiceBase EntityService;

    public EntityServiceNode EntityServiceNode;

    public PlayerInventoryServiceNode PlayerInventoryServiceNode;

    /// <summary>实体节点行为,定义不同策略下的实体同步行为</summary>
    public EntityBehaviorBase EntityBehavior;

    public WorldServiceBase(World world)
    {
        this.World = world;
        LoadWorldProfile();
    }

    public void InitializeNode()
    {
        World.AddChild(ChunkServiceNode = new ChunkServiceNode(World));
        World.AddChild(PlayerServiceNode = new PlayerServiceNode(World));
        World.AddChild(PlayerInventoryServiceNode = new PlayerInventoryServiceNode(World));
        World.AddChild(EntityServiceNode = new EntityServiceNode(World));
    }

    public abstract void InitializeServices();

    public void Dispose()
    {
        World?.Dispose();
        ChunkService?.Dispose();
        ChunkServiceNode?.Dispose();
        PlayerService?.Dispose();
        PlayerServiceNode?.Dispose();
        EntityServiceNode?.Dispose();
        PlayerInventoryServiceNode?.Dispose();
    }

    public virtual void LoadWorldProfile()
    {
        if (World.WorldName == "") return;
        using (var conn = SqliteTool.InitSqlite(World.WorldName))
        {
            if (conn.CheckWorldProfileExists("WorldProfile"))
            {
                Profile = conn.GetWorldProfileByteData("WorldProfile");
                Profile.LoadDate = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                GD.Print("加载WorldProfile");
                conn.UpdateWorldProfileByteData("WorldProfile", Profile);
            }
            else
            {
                Profile = new WorldProfile()
                {
                    WorldName = World.WorldName,
                    WorldSeed = World.Seed,
                    CreateDate = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    LoadDate = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                };
                GD.Print($"新建WorldProfile at {World.WorldName}");
                conn.InsertWorldProfileByteValue("WorldProfile", Profile);
            }
        }
    }

    public bool IsDay()
    {
        int h = GetTimeHour();
        return h is >= 6 and < 20;
    }

    public int GetTimeHour()
    {
        return (int)(((float)TickTimes % (float)DayTicks) / (float)DayTicks * 24f);
    }

    public int GetTimeDay()
    {
        return (int)(((float)TickTimes / (float)DayTicks));
    }
}