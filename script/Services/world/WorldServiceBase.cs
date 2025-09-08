using horizoncraft.script;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.rpc;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using HorizonCraft.script.Services.player;

namespace HorizonCraft.script.Services.world;

/// <summary>
/// 世界服务基础
/// </summary>
public abstract class WorldServiceBase
{
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
    


    public WorldServiceBase(World world)
    {
        this.World = world;
    }

    public void InitializeNode()
    {
        World.AddChild(ChunkServiceNode = new ChunkServiceNode(World));
        World.AddChild(PlayerServiceNode = new PlayerServiceNode(World));
        World.AddChild(PlayerInventoryServiceNode = new PlayerInventoryServiceNode(World));
        World.AddChild(EntityServiceNode = new EntityServiceNode(World));
    }

    public abstract void InitializeServices();
}