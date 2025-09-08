using Godot;
using horizoncraft.script;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using HorizonCraft.script.Services.player;

namespace HorizonCraft.script.Services.world;

public class PreviewWorldService : WorldServiceBase
{
    public PreviewWorldService(World world) : base(world)
    {
    }

    public override void InitializeServices()
    {
        ChunkService = new PreviewChunkService(World);
        PlayerService = new PreviewPlayerService(World);
        EntityService = new ClientEntityService(World);
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(PreviewWorldService)}");
    }
}