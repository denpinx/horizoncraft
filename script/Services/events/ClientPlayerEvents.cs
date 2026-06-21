using Horizoncraft.script.Events;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Net;
using Horizoncraft.script.rpc;
using Horizoncraft.script.Services.world;

namespace Horizoncraft.script.Services.Events;

//本地事件触发
public class ClientPlayerEvents : PlayerEvents
{
    public override bool PickItem(PlayerPickItemEvent e)
    {
        if (e.Inventory is BlockInventory)
        {
            e.world.Service.PlayerInventoryServiceNode.RpcId(1,
                nameof(PlayerInventoryServiceNode.PickBlockInvItem),
                e.Player.Name,
                e.Index,
                e.ActionType);
        }
        else
        {
            e.world.Service.PlayerInventoryServiceNode.RpcId(1,
                nameof(PlayerInventoryServiceNode.PickInvItem),
                e.Player.Name,
                e.Index,
                e.ActionType);
        }

        return false;
    }

    public override bool BreakBlock(PlayerBreakblockEvent e)
    {
        e.world.Service.PlayerServiceNode.RpcId(1,
            nameof(PlayerServiceNode.BreakBlockEvent),
            e.Player.Name,
            e.Position.X,
            e.Position.Y,
            e.Position.Z);
        return base.BreakBlock(e);
    }

    public override bool PlaceBlock(PlayerPlaceBlockEvent e)
    {
        e.world.Service.PlayerServiceNode.RpcId(1,
            nameof(PlayerServiceNode.PlaceBlockEvent),
            e.Player.Name,
            e.Position.X,
            e.Position.Y,
            e.Position.Z);
        return base.PlaceBlock(e);
    }

    public override bool UseItem(PlayerUseItemEvent e)
    {
        e.world.Service.PlayerServiceNode.RpcId(1,
            nameof(PlayerServiceNode.PlayerUseItemEvent),
            e.Player.Name,
            e.Position.X,
            e.Position.Y,
            e.Position.Z
        );
        return base.UseItem(e);
    }

    public override bool OpenBlockView(PlayerOpenBlockViewEvent e)
    {
        e.world.Service.PlayerInventoryServiceNode.RpcId(1,
            nameof(PlayerInventoryServiceNode.OpenBlockInv),
            e.Player.Name,
            e.Position.X,
            e.Position.Y,
            e.Position.Z);
        return false;
    }

    public override void CloseInventory(WorldServiceBase service, string name)
    {
        service.PlayerInventoryServiceNode.RpcId(1, nameof(PlayerInventoryServiceNode.CloseInventory), name);
    }

    public override void CraftGridRecipeItem(PlayerCraftItemEvent playerCraftItemEvent)
    {
        playerCraftItemEvent.world.Service.PlayerInventoryServiceNode.RpcId(1,
            nameof(PlayerInventoryServiceNode.CraftGridRecipeItem),
            playerCraftItemEvent.Player.Name, playerCraftItemEvent.IsAllCraft);
        base.CraftGridRecipeItem(playerCraftItemEvent);
    }

    public override void SetOpenBlockComponent(PlayerSetBlockComponentEvent e)
    {
        e.world.Service.ChunkServiceNode.RpcId(1,
            nameof(ChunkServiceNode.SetOpenBlockComponent),
            e.Player.Name, ByteTool.ToBytes(e.ComponentData));
    }

    public override void DropItem(WorldServiceBase service, string name)
    {
        service.PlayerServiceNode.RpcId(1, nameof(PlayerServiceNode.DropItemEvent), name, false);
    }

    public override void DropAllItem(WorldServiceBase service, string name)
    {
        service.PlayerServiceNode.RpcId(1, nameof(PlayerServiceNode.DropItemEvent), name, true);
    }

    public override void QuickClickBlockInvItem(WorldServiceBase service, string player_name, int index)
    {
        service.PlayerInventoryServiceNode.RpcId(1, nameof(PlayerInventoryServiceNode.QuickClickBlockInvItem),
            player_name, index);
    }

    public override void QuickClickPlayerInvItem(WorldServiceBase service, string player_name, int index)
    {
        service.PlayerInventoryServiceNode.RpcId(1, nameof(PlayerInventoryServiceNode.QuickClickPlayerInvItem),
            player_name, index);
    }
}