using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.rpc;
using HorizonCraft.script.Services.player;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.NewProxy.player;

public class HostPlayerService : PlayerServiceBase
{
    public HostPlayerService(World world) : base(world)
    {
        world.timer.Timeout += Ticking;
    }

    public void Ticking()
    {
        SyncPlayers();
    }

    public void SyncPlayers()
    {
        Dictionary<long, PlayerPack> packs = new();
        foreach (var Fs in Players)
        {
            PlayerData player = Fs.Value;
            if (player.Name == PlayerNode.Profile.Name) continue;
            var List = GetPlayersByRange(player, world.Service.ChunkService._loadrange);
            foreach (var target in List)
            {
                //if (target.Moved)
                {
                    if (!packs.ContainsKey(player.PeerId)) packs.Add(player.PeerId, new PlayerPack());
                    packs[player.PeerId].players.Add(new PlayerDataSnapshot(target));
                }
            }
        }

        //重置
        foreach (var playerData in Players.Values) playerData.Update = false;

        foreach (var sets in packs)
        {
            world.Service.PlayerServiceNode.RpcId(sets.Key,
                nameof(PlayerServiceNode.ReceivePlayerDatas),
                ByteTool.ToBytes<PlayerPack>(sets.Value)
            );
        }


        foreach (var player in Players.Values)
        {
            if (player.Name != PlayerNode.Profile.Name)
            {
                if (player.Inventory.update)
                {
                    player.Inventory.update = false;
                    world.Service.PlayerServiceNode.RpcId(
                        player.PeerId,
                        nameof(PlayerServiceNode.ReceivePlayerInv),
                        ByteTool.ToBytes<PlayerInventory>(player.Inventory)
                    );
                }

                if (player.OpeningBlockInventory)
                {
                    var pos = new Vector3I((int)player.OpenInventory.X, (int)player.OpenInventory.Y,
                        (int)player.OpenInventory.Z);
                    var blockdata = world.Service.ChunkService.GetBlock(pos);
                    var inv = blockdata?.GetComponent<InventoryComponent>();
                    if (blockdata != null && inv != null && inv.GetInventory().update)
                    {
                        world.Service.ChunkServiceNode.RpcId(player.PeerId,
                            nameof(ChunkServiceNode.ReciveLookingBlockData),
                            ByteTool.ToBytes<Blockdata>(blockdata),
                            ByteTool.ToBytes<PlayerInventory>(player.Inventory));
                    }
                    else
                    {
                        player.OpeningBlockInventory = player.OpeningBlockInventory = false;
                    }
                }
            }
        }
    }
}