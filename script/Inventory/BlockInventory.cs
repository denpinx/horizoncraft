using MemoryPack;

namespace Horizoncraft.script.Inventory;

[MemoryPackable]
public partial class BlockInventory : InventoryBase
{
    public BlockInventory(int Size) : base(Size)
    {
    }
}