using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script;
using horizoncraft.script.Entity;
using horizoncraft.script.I18N;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.rpc;
using horizoncraft.script.Services;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using horizoncraft.script.Services.message;
using HorizonCraft.script.Services.player;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.world;

/// <summary>
/// 世界服务基础，生命周期管理
/// </summary>
public abstract class WorldServiceBase
{
    public WorldProfile Profile;
    public World World;

    /// <summary>
    /// 定义多少Tick为一天
    /// </summary>
    const int DayTicks = 20 * 60;

    const int AutoSaveSeconds = 15;

    /// <summary>
    /// 当前世界的总时刻
    /// </summary>
    public int TickTimes;

    /// <summary>生命周期管理 </summary>
    public List<ServiceBase> Services = new();

    /// <summary>区块服务</summary>
    public ChunkServiceBase ChunkService;

    public ChunkServiceNode ChunkServiceNode;

    /// <summary>玩家服务</summary>
    public PlayerServiceBase PlayerService;

    public PlayerServiceNode PlayerServiceNode;

    /// <summary>实体服务</summary>
    public EntityServiceBase EntityService;

    public EntityServiceNode EntityServiceNode;

    /// <summary>消息服务</summary>
    public MessageServiceBase MessageService;

    public MessageServiceNode MessageServiceNode;

    public PlayerInventoryServiceNode PlayerInventoryServiceNode;

    /// <summary>实体节点行为,定义不同策略下的实体同步行为</summary>
    public EntityBehaviorBase EntityBehavior;


    private Stopwatch _stopwatch = new Stopwatch();

    public WorldServiceBase(World world)
    {
        this.World = world;
        LoadWorldProfile();
        BiomeManage.Reset();
        world.timer.Timeout += WorldServiceTick;
    }


    private void WorldServiceTick()
    {
        //自动保存
        if (TickTimes % (20 * AutoSaveSeconds) == 1)
            Save();
    }


    public void InitializeNode()
    {
        World.AddChild(ChunkServiceNode = new ChunkServiceNode(World));
        World.AddChild(PlayerServiceNode = new PlayerServiceNode(World));
        World.AddChild(PlayerInventoryServiceNode = new PlayerInventoryServiceNode(World));
        World.AddChild(EntityServiceNode = new EntityServiceNode(World));
        World.AddChild(MessageServiceNode = new MessageServiceNode(World));
    }

    public abstract void InitializeServices();

    /// <summary>
    /// 将生命周期交给 WorldServiceBase管理
    /// </summary>
    /// <param name="service">服务</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>服务本身</returns>
    public T AddService<T>(ServiceBase service) where T : ServiceBase
    {
        Services.Add(service);
        return (T)service;
    }


    /// <summary>
    /// 保存所有实现了 ISave接口的服务。
    /// </summary>
    public async void Save()
    {
        List<Task> tasks = new();
        _stopwatch.Restart();
        foreach (ServiceBase service in Services)
        {
            if (service is ISave save)
            {
                Task task = new Task(() => save.SaveAll());
                tasks.Add(task);
                task.Start();
            }
        }

        await Task.WhenAll(tasks.ToArray());
        _stopwatch.Stop();

        GD.Print($"[{GetTime()}]保存结束。用时:{_stopwatch.Elapsed.Milliseconds}μs");
    }

    /// <summary>
    /// 加载世界文档
    /// </summary>
    public virtual void LoadWorldProfile()
    {
        if (World.WorldName == "") return;
        using (var conn = SqliteTool.InitSqlite(World.WorldName))
        {
            if (conn.CheckWorldProfileExists("WorldProfile"))
            {
                Profile = conn.GetWorldProfileByteData("WorldProfile");
                Profile.LoadDate = DateTime.Now.ToString(CultureInfo.InvariantCulture);
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

    /// <summary>
    /// 是否为白天
    /// </summary>
    /// <returns></returns>
    public bool IsDay()
    {
        int h = (int)GetTimeHour();
        return h is >= 6 and < 20;
    }

    public float GetTimeMinute()
    {
        return (float)(TickTimes % ((float)DayTicks / 24));
    }

    /// <summary>
    /// 获取当前的世界小时数
    /// </summary>
    /// <returns></returns>
    public float GetTimeHour()
    {
        return (((float)TickTimes % (float)DayTicks) / (float)DayTicks * 24f);
    }

    /// <summary>
    /// 获取当前的世界天数
    /// </summary>
    /// <returns></returns>
    public int GetTimeDay()
    {
        return (int)(((float)TickTimes / (float)DayTicks));
    }

    public string GetTime()
    {
        return "world_time".Trprefix("debug", GetTimeDay(), (int)GetTimeHour(), (int)GetTimeMinute());
    }

    /// <summary>
    /// 获取当前时间对应的光线明暗变化
    /// </summary>
    /// <returns></returns>
    public byte GetLightChange()
    {
        float hour = GetTimeHour();
        if (hour < 5f || hour >= 20f)
            return 200;
        else if (hour >= 5f && hour < 8f)
            return (byte)(200 * (1f - (hour - 5f) / 3f));
        else if (hour >= 8f && hour < 17f)
            return 0;
        else
            return (byte)(200 * ((hour - 17f) / 3f));
    }

    /// <summary>
    /// 获取当前时间的天空颜色渐变
    /// </summary>
    /// <returns>天空颜色</returns>
    public Color GetSkyColor()
    {
        float hour = GetTimeHour();

        if (hour >= 0 && hour < 6)
        {
            float p = hour / 6f;
            byte r = (byte)Math.Clamp(15 + p * (25 - 15), 0, 255);
            byte g = (byte)Math.Clamp(25 + p * (55 - 25), 0, 255);
            byte b = (byte)Math.Clamp(55 + p * (135 - 55), 0, 255);
            return Color.Color8(r, g, b);
        }

        if (hour >= 6 && hour < 12)
        {
            float p = (hour - 6f) / 6f;
            byte r = (byte)Math.Clamp(25 + p * (135 - 25), 0, 255);
            byte g = (byte)Math.Clamp(55 + p * (175 - 55), 0, 255);
            byte b = (byte)Math.Clamp(135 + p * (255 - 135), 0, 255);
            return Color.Color8(r, g, b);
        }

        if (hour >= 12 && hour < 18)
        {
            float p = (hour - 12f) / 6f;
            byte r = (byte)Math.Clamp(135 + p * (255 - 135), 0, 255);
            byte g = (byte)Math.Clamp(175 + p * (135 - 175), 0, 255);
            byte b = (byte)Math.Clamp(255 + p * (0 - 255), 0, 255);
            return Color.Color8(r, g, b);
        }

        if (hour >= 18 && hour < 24)
        {
            float p = (hour - 18f) / 6f;
            byte r = (byte)Math.Clamp(255 + p * (15 - 255), 0, 255);
            byte g = (byte)Math.Clamp(135 + p * (25 - 135), 0, 255);
            byte b = (byte)Math.Clamp(0 + p * (55 - 0), 0, 255);
            return Color.Color8(r, g, b);
        }

        return Color.Color8(255, 255, 255);
    }
}