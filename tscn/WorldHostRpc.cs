using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using Godot.Collections;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.Tool;
using MemoryPack;

namespace horizoncraft.script;

//客户端RPC函数
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
            var bytes = PlayerData.ToBytes(playerData);
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
            PlayerData playerData = PlayerData.FromBytes(bytes);
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdateChunk(int x, int y)
    {
        var pos = new Vector2I(x, y);
        if (WorldService.Chunks.ContainsKey(pos))
        {
            WorldService.Chunks[pos].update_server = true;
        }
    }
}