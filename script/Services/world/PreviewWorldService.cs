using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.Recipes;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.entity;
using Horizoncraft.script.Services.message;
using Horizoncraft.script.Services.player;
using Horizoncraft.script.WorldControl;
using Microsoft.Extensions.DependencyInjection;

namespace Horizoncraft.script.Services.world;

public class PreviewWorldService : WorldServiceBase
{
    public PreviewWorldService(World world) : base(world)
    {
        ServiceCollection.AddTransient<NeoComponentManager, NeoComponentManager>();
        ServiceCollection.AddTransient<NeoWorldGenerator, NeoWorldGenerator>();
        ServiceCollection.AddTransient<NeoLootTable, NeoLootTable>();
        ServiceCollection.AddTransient<NeoRecipeManage, NeoRecipeManage>();
        ServiceCollection.AddTransient<ChunkServiceBase, PreviewChunkService>();
        ServiceCollection.AddTransient<PlayerServiceBase, PreviewPlayerService>();
        ServiceCollection.AddTransient<EntityServiceBase, HostEntityService>();
        ServiceCollection.AddTransient<MessageServiceBase, SingleMessageService>();
        ServiceCollection.AddTransient<EntityBehaviorBase, EntityBehaviorBase>();
        
        ServiceProvider = ServiceCollection.BuildServiceProvider();
    }
    
    public override void LoadWorldProfile()
    {
    }
}