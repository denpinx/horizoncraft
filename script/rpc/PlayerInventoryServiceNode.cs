using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Events.player;
using HorizonCraft.script.Services.world;

namespace horizoncraft.script.rpc;

/// <summary>
/// 玩家容器相关RPC操作
/// </summary>
public partial class PlayerInventoryServiceNode : Node
{
    private World World;

    private WorldServiceBase WorldService
    {
        get { return World.Service; }
    }

    public PlayerInventoryServiceNode(World world)
    {
        World = world;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PickBlockInvItem(string player, int index, int ActionType)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerData))
        {
            if (playerData.OpeningBlockInventory)
            {
                var pos = playerData.OpenInventory;
                var inv = WorldService.ChunkService.GetBlock(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z))
                    ?.GetComponent<InventoryComponent>()?.GetInventory();
                if (inv == null) return;
                var ev = new PlayerPickItemEvent()
                {
                    world = World,
                    Player = playerData,
                    Index = index,
                    ActionType = ActionType,
                    Inventory = inv
                };
                WorldService.PlayerService.Events.PickItem(ev);
            }
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OpenBlockInv(string player, int x, int y, int z)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerData))
        {
            playerData.OpeningBlockInventory = true;
            playerData.OpenInventory = new System.Numerics.Vector3(x, y, z);
        }
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void CloseInventory(string player)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerData))
        {
            playerData.OpeningBlockInventory = false;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PickInvItem(string player, int index, int ActionType)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerData))
        {
            var ppi = new PlayerPickItemEvent()
            {
                world = World,
                Player = playerData,
                Index = index,
                ActionType = ActionType,
                Inventory = playerData.Inventory
            };
            WorldService.PlayerService.Events.PickItem(ppi);
            playerData.Inventory.update = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetHandSlot(string player, int slot)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerData))
        {
            playerData.Inventory.HandSlot = (short)slot;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void CraftGridRecipeItem(string player, bool all)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerData))
        {
            var cgri = new PlayerCraftItemEvent()
            {
                world = World,
                Player = playerData,
                IsAllCraft = all
            };
            WorldService.PlayerService.Events.CraftGridRecipeItem(cgri);
        }
    }
}