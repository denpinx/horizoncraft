using System;
using Godot;
using Godot.NativeInterop;
using Horizoncraft.script.Net;
using Horizoncraft.script.Services.Events;
using HorizonCraft.script.Services.player;
using HorizonCraft.script.Services.world;

namespace Horizoncraft.script.NewProxy.player;

public class ClientPlayerService : PlayerServiceBase
{
    private Vector2I LastPosition;

    public ClientPlayerService(World world) : base(world)
    {
        Events = new ClientPlayerEvents();
    }

    public override void Ticking()
    {
        base.Ticking();
        var player = World.PlayerNode.playerData;
        if (player != null)
        {
            if (LastPosition != player.Position_v2i)
            {
                var snap = new PlayerDataSnapshot(player);
                World.Service.PlayerServiceNode.RpcId(1,
                    nameof(PlayerServiceNode.UpdataPlayer),
                    World.PlayerNode.Name,
                    ByteTool.ToBytes(snap));
            }

            LastPosition = player.Position_v2i;
        }
    }

    public override bool GetPlayerOrLoad(string name, out PlayerData playerData)
    {
        if (Players.TryGetValue(name, out var player))
        {
            playerData = player;
            return true;
        }

        if (ClientWorldService.Connected)
            World.Service.PlayerServiceNode.RpcId(1,
                nameof(PlayerServiceNode.GetPlayer), name, World.Multiplayer.GetUniqueId()
            );
        playerData = null;
        return false;
    }

    public override void SaveAll()
    {
    }

    public override void SavePlayer(PlayerData player)
    {
    }
}