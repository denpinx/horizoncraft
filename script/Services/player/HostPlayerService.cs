using System.Collections.Generic;
using System.Linq;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Expand;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.rpc;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.player;

public class HostPlayerService : PlayerServiceBase
{
    public HostPlayerService(World world) : base(world)
    {
    }

    public override void Ticking()
    {
        base.Ticking();
        SyncPlayers();
    }

    public void SyncPlayers()
    {
        SyncPlayerDatas();
        SyncPlayerInventory();
    }

    /// <summary>
    /// 同步玩家位置
    /// </summary>
    private void SyncPlayerDatas()
    {
        Dictionary<long, PlayerPack> packs = new();
        foreach (var Fs in Players)
        {
            PlayerData player = Fs.Value;
            if (player.Name == PlayerNode.Profile.Name) continue;

            var List = GetPlayersByRange(player, World.Service.ChunkService.LoadHorizon);
            foreach (var target in List)
            {
                if (target.Update)
                {
                    if (!packs.ContainsKey(player.PeerId)) packs.Add(player.PeerId, new PlayerPack());
                    packs[player.PeerId].players.Add(new PlayerDataSnapshot(target));
                }
            }
        }

        //重置更新标签
        foreach (var playerData in Players.Values) playerData.Update = false;
        foreach (var sets in packs)
        {
            World.Service.PlayerServiceNode.RpcId(sets.Key,
                nameof(PlayerServiceNode.ReceivePlayerDatas),
                ByteTool.ToBytes<PlayerPack>(sets.Value)
            );
        }
    }

    /// <summary>
    /// 同步玩家的物品栏，以及玩家打开的方块物品栏
    /// </summary>
    private void SyncPlayerInventory()
    {
        foreach (var player in Players.Values)
        {
            if (player.Name != PlayerNode.Profile.Name)
            {
                if (player.Inventory.update)
                {
                    player.Inventory.update = false;
                    if (World.Multiplayer.GetPeers().Contains(player.PeerId))
                        World.Service.PlayerServiceNode.RpcId(
                            player.PeerId,
                            nameof(PlayerServiceNode.ReceivePlayerInv),
                            ByteTool.ToBytes<PlayerInventory>(player.Inventory)
                        );
                }

                if (player.OpeningBlockInventory)
                {
                    var pos = new Vector3I((int)player.OpenInventory.X, (int)player.OpenInventory.Y,
                        (int)player.OpenInventory.Z);
                    var blockdata = World.Service.ChunkService.GetBlock(pos);
                    var inv = blockdata?.GetComponent<InventoryComponent>();
                    if (blockdata != null && inv != null)
                    {
                        if (inv.GetInventory().update)
                            World.Service.ChunkServiceNode.RpcId(player.PeerId,
                                nameof(ChunkServiceNode.ReciveLookingBlockData),
                                ByteTool.ToBytes<BlockData>(blockdata),
                                ByteTool.ToBytes<PlayerInventory>(player.Inventory));
                    }
                    else
                    {
                        player.OpeningBlockInventory = false;
                    }
                }
            }
        }

        //必须要先在处理完其他所有玩家的打开的方块容器再重置标签，因为一个容器可能被多个人查看
        foreach (var player in Players.Values)
        {
            if (player.OpeningBlockInventory)
            {
                var pos = new Vector3I(
                    (int)player.OpenInventory.X,
                    (int)player.OpenInventory.Y,
                    (int)player.OpenInventory.Z
                );
                var blockdata = World.Service.ChunkService.GetBlock(pos);
                var inv = blockdata?.GetComponent<InventoryComponent>();
                if (blockdata != null && inv != null && inv.GetInventory().update)
                {
                    inv.GetInventory().update = false;
                }
            }
        }
    }

    public override void OnPlayerRespawn(PlayerData playerData)
    {
        if (playerData.Name == PlayerNode.Profile.Name)
        {
            World.PlayerNode.Position = playerData.Position.ToGodotVector2();
        }
        else
        {
            var snap = new PlayerDataSnapshot(playerData);
            World.Service.PlayerServiceNode.RpcId(playerData.PeerId, nameof(PlayerServiceNode.UpdataPlayer),
                ByteTool.ToBytes(snap));
        }
    }
}