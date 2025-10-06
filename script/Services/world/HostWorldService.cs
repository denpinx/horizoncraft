using System;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Entity;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using horizoncraft.script.Services.message;
using HorizonCraft.script.Services.player;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.world;

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
    }

    public override void InitializeServices()
    {
        EntityBehavior = new EntityBehaviorBase();
        ChunkService = new HostChunkService(World);
        PlayerService = new HostPlayerService(World);
        EntityService = new HostEntityService(World);
        MessageService = new HostMessageService(World);
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(HostWorldService)}");
    }

    private void OnPlayerExit(long id)
    {
        foreach (var player in PlayerService.Players.Values)
        {
            if (player.PeerId == id)
            {
                PlayerService.Players.TryRemove(player.Name, out var _);
                return;
            }
        }
    }
}