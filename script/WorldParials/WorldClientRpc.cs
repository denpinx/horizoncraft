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
        if (player.ShowView != null && player.playerData.OpenInventory == new System.Numerics.Vector3(x, y, z))
        {
            player.ShowView.TargetBlock = block;
        }
        else if (player.ShowView != null)
        {
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveOpenBlockData(byte[] data, byte[] playerinv)
    {
        Blockdata blockdata = ByteTool.FromBytes<Blockdata>(data);
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(playerinv);
        player.playerData.Inventory = inv;

        if (player.ShowView != null)
        {
            player.ShowView.TargetBlock = blockdata;
        }
        else
        {
            if (player.playerData.OpeningBlockInventory && WorldService is WorldClientService wcs)
            {
                GD.Print("收到数据,打开菜单");
                wcs.OpenBlockView(blockdata);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RecivePlayerInv(byte[] data)
    {
        if (player.playerData == null) return;
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(data);
        player.playerData.Inventory = inv;
        if (player.ShowView != null)
        {
            
        }
    }
}