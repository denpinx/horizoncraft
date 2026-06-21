using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.Recipes;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.entity;
using Horizoncraft.script.Services.Message;
using Horizoncraft.script.Services.player;
using Horizoncraft.script.WorldControl;
using Microsoft.Extensions.DependencyInjection;

namespace Horizoncraft.script.Services.world;

public class SingleWorldService : WorldServiceBase
{
    public SingleWorldService(World world) : base(world)
    {
        ServiceCollection.AddTransient<NeoComponentManager, NeoComponentManager>();
        ServiceCollection.AddTransient<NeoWorldGenerator, NeoWorldGenerator>();
        ServiceCollection.AddTransient<NeoLootTable, NeoLootTable>();
        ServiceCollection.AddTransient<NeoRecipeManage, NeoRecipeManage>();
        ServiceCollection.AddTransient<ChunkServiceBase, SingleChunkService>();
        ServiceCollection.AddTransient<PlayerServiceBase, SinglePlayerService>();
        ServiceCollection.AddTransient<EntityServiceBase, HostEntityService>();
        ServiceCollection.AddTransient<MessageServiceBase, SingleMessageService>();
        ServiceCollection.AddTransient<EntityBehaviorBase, EntityBehaviorBase>();
        ServiceProvider = ServiceCollection.BuildServiceProvider();
    }
}