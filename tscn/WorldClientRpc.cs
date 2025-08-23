using System.IO;
using System.IO.Compression;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;

namespace horizoncraft.script;

public partial class World
{
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunk(byte[] data)
    {
        Chunk chunk = ByteTool.FromBytes<Chunk>(data);
        WorldService.Chunks[new(chunk.X, chunk.Y)] = chunk;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunkPack(byte[] data)
    {
        ChunkPack sync = ByteTool.FromBytes<ChunkPack>(data);
        for (int i = 0; i < sync.Chunks.Count; i++)
            WorldService.Chunks[sync.Chunks[i].coord] = sync.Chunks[i];
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunkUpdatePack(byte[] data)
    {
        if (WorldService is WorldClientService wcs)
        {
            wcs.ReciveChunkPacks.Enqueue(data);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveWorldTime(int time)
    {
        WorldService.TickTimes = time;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RecivePlayer(byte[] data)
    {
        PlayerData playerData = ByteTool.FromBytes<PlayerData>(data);
        if (playerData.Name == Player.Profile.Name)
        {
            if (player.playerData == null)
            {
                player.playerData = playerData;
                player.playerData.player = player;
                if (WorldService is WorldClientService wcs)
                {
                    player.Position = playerData.Position_v2;
                }
            }
            else
            {
                if (player.ShowView != null)
                {
                    player.ShowView.PlayerInvBase = playerData.Inventory;
                }
            }
        }

        WorldService.Players[playerData.Name] = playerData;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RecivePlayerDatas(byte[] data)
    {
        PlayerPack playerPack = ByteTool.FromBytes<PlayerPack>(data);
        for (int i = 0; i < playerPack.players.Count; i++)
        {
            var player = playerPack.players[i];
            if (!WorldService.Players.ContainsKey(player.Name))
            {
                WorldService.Players.TryAdd(player.Name, new PlayerData()
                {
                    Name = player.Name,
                });
            }

            var pd = WorldService.Players[player.Name];
            pd.Position = new Vector2(player.x, player.y);
        }

        //player.playerData = playerPack.SelfData;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveBlockData(byte[] data, int x, int y, int z)
    {
        Blockdata block = ByteTool.FromBytes<Blockdata>(data);
        WorldService.SetBlock(new Vector3I(x, y, z), block);

        if (player.ShowView != null && player.ShowView.TargetBlockGlobalPos == new Vector3I(x, y, z))
            player.ShowView.TargetInvBase = block.GetComponent<InventoryComponent>().GetInventory();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveBlockInventoryData(byte[] data, byte[] playerinv)
    {
        InventoryComponent inventory = ByteTool.FromBytes<InventoryComponent>(data);
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(playerinv);
        if (player.ShowView != null)
        {
            player.ShowView.TargetInvBase = inventory.GetInventory();
            player.ShowView.PlayerInvBase = inv;
        }

        else
        {
            if (player.playerData.OpeningBlockInventory && WorldService is WorldClientService wcs)
            {
                wcs.OpenBlockView(inventory);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RecivePlayerInv(byte[] data)
    {
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(data);
        player.playerData.Inventory = inv;
        if (player.ShowView != null)
        {
            player.ShowView.PlayerInvBase = inv;
        }
    }
}