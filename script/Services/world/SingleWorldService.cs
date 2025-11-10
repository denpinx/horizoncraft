using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Entity;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.entity;
using Horizoncraft.script.Services.message;
using Horizoncraft.script.Services.player;

namespace Horizoncraft.script.Services.world;

public class SingleWorldService : WorldServiceBase
{
    public SingleWorldService(World world) : base(world)
    {
    }

    public override void InitializeServices()
    {
        EntityBehavior = new EntityBehaviorBase();
        ChunkService = AddService<SingleChunkService>(new SingleChunkService(World));
        PlayerService = AddService<SinglePlayerService>(new SinglePlayerService(World));
        EntityService = AddService<HostEntityService>(new HostEntityService(World));
        MessageService = AddService<SingleMessageService>(new SingleMessageService(World));
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(SingleWorldService)}");
    }
}