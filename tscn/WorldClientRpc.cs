using System.IO;
using System.IO.Compression;
using Godot;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using MemoryPack;

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
        if (WorldService is WorldBase worldBase)
        {
            PlayerData playerData = ByteTool.FromBytes<PlayerData>(data);
            if (playerData.Name == Player.Profile.Name && player.playerData == null)
            {
                player.playerData = playerData;
                player.playerData.player = player;
                if (WorldService is WorldClientService wcs)
                {
                    player.Position = playerData.Position_v2;
                }
            }

            worldBase.Players[playerData.Name] = playerData;
        }
    }
}