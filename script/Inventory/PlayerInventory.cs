using MemoryPack;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
public partial class PlayerInventory : InventoryBase
{
    public short HandSlot = 0;

    public PlayerInventory()
    {
        Size = 4 * 9;
        Items = new ItemStack[Size];
    }
}