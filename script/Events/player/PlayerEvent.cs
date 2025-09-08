using Godot;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.player;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events.player;

public class PlayerEvent
{
    public World world;

    public PlayerServiceBase PlayerService
    {
        get { return world.Service.PlayerService; }
    }

    public ChunkServiceBase ChunkService
    {
        get { return world.Service.ChunkService; }
    }

    public PlayerData Player;
}

public class PlayerCraftItemEvent : PlayerEvent
{
    public bool IsAllCraft = false;
}

public class PlayerSetBlockComponentEvent : PlayerEvent
{
    public SetComponentData ComponentData;
}

public class PlayerOpenBlockViewEvent : PlayerEvent
{
    public string ViewName;
    public Vector3I Position;
}

public class PlayerPickItemEvent : PlayerEvent
{
    public InventoryBase Inventory;
    public int Index;
    public int ActionType;

    public ItemStack GetIndexItem()
        => Inventory.GetItem(Index);

    public void SetIndexItem(ItemStack item)
        => Inventory.SetItem(Index, item);
}

public class PlayerPlaceBlockEvent : PlayerEvent
{
    public Vector3I Position;
    public bool coercive = false;
    public bool IsCollide = false;
}

public class PlayerBreakblockEvent : PlayerEvent
{
    public Vector3I Position;

    public ItemStack GetItemStack()
        => Player.Inventory.GetHandItemStack();

    public BlockData GetBlockData()
        => ChunkService.GetBlock(Position);

    public ItemStack DropItem;
}
public class InterfaceBlockEvent : PlayerEvent
{
    public Vector3I Position;
}