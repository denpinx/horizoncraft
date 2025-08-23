using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using Godot.Collections;
using horizoncraft.script.Components;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.Tool;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

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
            var bytes = ByteTool.ToBytes(playerData);
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
            PlayerdataSnapshot playerData = ByteTool.FromBytes<PlayerdataSnapshot>(bytes);
            worldBase.Players[name].Position = new Vector2(playerData.x, playerData.y);
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
    public void SetOpenBlockComponent(string name, byte[] bytes)
    {
        if (!WorldService.Players.ContainsKey(name)) return;

        var player = WorldService.Players[name];
        var scd = ByteTool.FromBytes<SetComponentData>(bytes);
        WorldService.SetOpenBlockComponent(player, scd);
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PickBlockInvItem(string player, int index)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        if (playerdata.OpeningBlockInventory)
        {
            var pos = playerdata.OpenInventory;
            var inv = WorldService.GetBlock(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z))
                ?.GetComponent<InventoryComponent>()?.GetInventory();
            if (inv == null) return;
            WorldService.PickItem(playerdata, inv, index);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OpenPlayerInv(string player)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        if (playerdata == null) return;
        playerdata.OpeningBlockInventory = true;
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OpenBlockInv(string player, int x, int y, int z)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        playerdata.OpeningBlockInventory = true;
        playerdata.OpenInventory = new Vector3(x, y, z);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void CloseBlockInv(string player)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        playerdata.OpeningBlockInventory = false;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PickInvItem(string player, int index)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        WorldService.PickItem(playerdata, playerdata.Inventory, index);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetHandSlot(string player, int slot)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        playerdata.Inventory.HandSlot = (short)slot;
    }
}