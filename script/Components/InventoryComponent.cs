using horizoncraft.script.Inventory;
using MemoryPack;

namespace horizoncraft.script.Components;

[MemoryPackable]
public partial class InventoryComponent : Component
{
    public string InventoryName = "";
    public int Size = 36;

    public BlockInventory Inventory;
    public BlockInventory GetInventory()
    {
        if (Inventory == null) Inventory = new BlockInventory(Size);
        return Inventory;
    }
}