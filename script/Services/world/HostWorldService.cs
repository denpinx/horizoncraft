using System;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Net;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.Recipes;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.entity;
using Horizoncraft.script.Services.message;
using Horizoncraft.script.Services.player;
using Horizoncraft.script.WorldControl;
using Horizoncraft.script.WorldControl.Tool;
using Microsoft.Extensions.DependencyInjection;

namespace Horizoncraft.script.Services.world;

public class HostWorldService : WorldServiceBase
{
    public static int Port = 9999;
    public static int MaxPlayer = 10;
    
    public HostWorldService(World world) : base(world)
    {
        world.Multiplayer.PeerDisconnected += OnPlayerExit;
        var enet = new ENetMultiplayerPeer();
        enet.CreateServer(Port, MaxPlayer);
        world.Multiplayer.MultiplayerPeer = enet;
        ServiceCollection.AddTransient<NeoComponentManager, NeoComponentManager>();
        ServiceCollection.AddTransient<NeoWorldGenerator, NeoWorldGenerator>();
        ServiceCollection.AddTransient<NeoLootTable, NeoLootTable>();
        ServiceCollection.AddTransient<NeoRecipeManage, NeoRecipeManage>();
        ServiceCollection.AddTransient<ChunkServiceBase, HostChunkService>();
        ServiceCollection.AddTransient<PlayerServiceBase, HostPlayerService>();
        ServiceCollection.AddTransient<EntityServiceBase, HostEntityService>();
        ServiceCollection.AddTransient<MessageServiceBase, HostMessageService>();
        ServiceCollection.AddTransient<EntityBehaviorBase, EntityBehaviorBase>();
        ServiceProvider = ServiceCollection.BuildServiceProvider();
    }
    
    private void OnPlayerExit(long id)
    {
        var PlayerService = ServiceProvider.GetService<PlayerServiceBase>();
        foreach (var player in PlayerService.Players.Values)
        {
            if (player.PeerId == id)
            {
                PlayerService.Players.TryRemove(player.Name, out var _);
                PlayerService.SavePlayer(player);
                return;
            }
        }
    }
}