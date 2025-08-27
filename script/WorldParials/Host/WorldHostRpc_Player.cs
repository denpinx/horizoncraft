using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Net;
using HorizonCraft.script.WorldControl.Service;

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
            worldBase.Players[name].Position = new System.Numerics.Vector2(playerData.x, playerData.y);
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
        playerdata.OpenInventory = new System.Numerics.Vector3(x, y, z);
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
        playerdata.Inventory.update = true;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetHandSlot(string player, int slot)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        playerdata.Inventory.HandSlot = (short)slot;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void CraftGridRecipeItem(string player)
    {
        if (!WorldService.Players.ContainsKey(player)) return;
        var playerdata = WorldService.Players[player];
        WorldService.CraftGridRecipeItem(playerdata);
    }
}