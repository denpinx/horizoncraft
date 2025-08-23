using Godot;
using MemoryPack;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
public partial class PlayerInventory : InventoryBase
{
    public short HandSlot = 0;
    public ItemStack HandItemStack;

    public PlayerInventory() : base(4 * 9)
    {
    }

    public void SubHandItemAmount(int amount = 0)
    {
        if (HandItemStack == null)
        {
            GD.PrintErr("SubHandItemAmount: HandItemStack is null");
            return;
        }

        HandItemStack.Amount -= amount;
        if (HandItemStack.Amount <= 0) HandItemStack = null;
    }
}