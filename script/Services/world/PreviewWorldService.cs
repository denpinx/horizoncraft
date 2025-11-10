using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Entity;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.entity;
using Horizoncraft.script.Services.message;
using Horizoncraft.script.Services.player;

namespace Horizoncraft.script.Services.world;

public class PreviewWorldService : WorldServiceBase
{
    public PreviewWorldService(World world) : base(world)
    {
    }

    public override void InitializeServices()
    {
        EntityBehavior = new EntityBehaviorBase();
        ChunkService = AddService<PreviewChunkService>(new PreviewChunkService(World));
        PlayerService = AddService<PreviewPlayerService>(new PreviewPlayerService(World));
        EntityService = AddService<ClientEntityService>(new ClientEntityService(World));
        MessageService = AddService<SingleMessageService>(new SingleMessageService(World));
        
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(PreviewWorldService)}");
    }

    public override void LoadWorldProfile()
    {
    }
}