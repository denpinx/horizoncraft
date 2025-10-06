using Godot;
using horizoncraft.script;
using horizoncraft.script.Entity;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using horizoncraft.script.Services.message;
using HorizonCraft.script.Services.player;

namespace HorizonCraft.script.Services.world;

public class PreviewWorldService : WorldServiceBase
{
    public PreviewWorldService(World world) : base(world)
    {
    }

    public override void InitializeServices()
    {
        EntityBehavior = new EntityBehaviorBase();
        ChunkService = new PreviewChunkService(World);
        PlayerService = new PreviewPlayerService(World);
        EntityService = new ClientEntityService(World);
        MessageService = new SingleMessageService(World);
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(PreviewWorldService)}");
    }

    public override void LoadWorldProfile()
    {
    }
}