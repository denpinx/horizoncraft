using Godot;
using horizoncraft.script;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using HorizonCraft.script.Services.player;

namespace HorizonCraft.script.Services.world;

public class SingleWorldService : WorldServiceBase
{
    public SingleWorldService(World world) : base(world)
    {

    }

    public override void InitializeServices()
    {
        ChunkService = new SingleChunkService(World);
        PlayerService = new SinglePlayerService(World);
        EntityService = new HostEntityService(World);
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(SingleWorldService)}");
    }
}