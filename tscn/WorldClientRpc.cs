using System.IO;
using System.IO.Compression;
using Godot;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using MemoryPack;

namespace horizoncraft.script;

public partial class World
{
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunk(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        Chunk chunk = MemoryPackSerializer.Deserialize<Chunk>(output.ToArray());
        WorldService.LoadedChunks[new(chunk.X, chunk.Y)] = chunk;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RecivePlayer(byte[] data)
    {
        if (WorldService is WorldBase worldBase)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);

            PlayerData playerData = MemoryPackSerializer.Deserialize<PlayerData>(output.ToArray());
            if (playerData.Name == Player.LocalName)
            {
                player.playerData = playerData;
                player.playerData.player = player;
                if (WorldService is WorldClientService wcs)
                {
                    player.Position = playerData.Position;
                }
            }
            else
            {
                
            }

            worldBase.Players[playerData.Name] = playerData;
            GD.Print($"[{worldBase.TickTimes}] RecivePlayer() Done");
        }
    }
}