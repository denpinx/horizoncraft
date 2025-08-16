using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using Godot.Collections;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using MemoryPack;

namespace horizoncraft.script;

public partial class World
{
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void GetPlayer(string name, int peerid)
    {
        GD.Print($"[请求获取玩家信息]{name},{peerid}");
        PlayerData playerData;
        if (WorldService is IWorldService iws && iws.GetPlayer(name, out playerData))
        {
            playerData.PeerId = peerid;
            var bytes = WorldManage.PlayerToByte(playerData);
            RpcId(peerid, "RecivePlayer", bytes);
            GD.Print($"[成功获取,即将返回]{name}");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdataPlayer(string name, byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            GD.Print("异常数据！");
        }

        ;
        if (WorldService is { } worldBase)
        {
            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            //
            PlayerData playerData = MemoryPackSerializer.Deserialize<PlayerData>(output.ToArray());
            worldBase.Players[name] = playerData;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdataPosition(string name, float x, float y)
    {
        PlayerData playerData;
        if (WorldService is IWorldService iws && iws.GetPlayer(name, out playerData))
            playerData.Position = new(x, y);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ConnectDone(string name, int peerid)
    {
        if (WorldService is { } worldbase)
            GD.Print($"[{worldbase.TickTimes}] ConnectDone({name},{peerid})");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OnMoveAChunk()
    {
        if (WorldService is IWorldService iws)
            iws.UpdateLoadChunkCoords();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetBlock(int x, int y, int z, int id, int state)
    {
        WorldService.SetBlock(new(x, y, z), Materials.Valueof(id), false, state);
    }
}