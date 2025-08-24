using Godot;
using horizoncraft.script.Net;
using MemoryPack;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
[MemoryPackUnion(0, typeof(PlayerInventory))]
[MemoryPackUnion(1, typeof(BlockInventory))]
public abstract partial class InventoryBase
{
    [MemoryPackIgnore] public bool update = true;
    public int Size = 0;
    public ItemStack[] Items;


    public InventoryBase(int size)
    {
        this.Size = size;
        Items = new ItemStack[Size];
    }


    public ItemStack GetItem(int id) => Items[id];

    public void SetItem(int id, ItemStack item)
    {
        if (item==null||item.Amount <= 0)
            Items[id] = null;
        else
            Items[id] = item;

        update = true;
    }

    public void SubItemAmount(int id, int amount = 1)
    {
        if (Items[id] == null)
        {
            GD.PrintErr("Inventory: SubItemAmount: Item not found!");
            return;
        }

        Items[id].Amount -= amount;
        if (Items[id].Amount <= 0) Items[id] = null;
    }

    public bool IsEmpty(int index)
    {
        return Items[index] == null;
    }

    public bool TryAddItem(ItemStack additem)
    {
        if (!HasSpace(additem)) return false;
        for (int i = 0; i < Size; i++)
        {
            ItemStack item = Items[i];
            if (item == null)
            {
                Items[i] = additem;
                update = true;
                return true;
            }
            else if (item.Id == additem.Id)
            {
                int max = item.GetItemMeta().MaxAmount;
                int space = max - item.Amount;
                //空间足够 
                if (space > 0 && space >= additem.Amount)
                {
                    item.Amount += additem.Amount;
                    additem.Amount = 0;
                    update = true;
                    return true;
                }

                //空间不足
                if (space > 0 && space < additem.Amount)
                {
                    item.Amount = max;
                    additem.Amount -= space;
                }
            }
        }

        return false;
    }

    private bool HasSpace(ItemStack additem)
    {
        int space = 0;
        for (int i = 0; i < Size; i++)
        {
            ItemStack item = Items[i];
            if (item == null)
                return true;
            if (item.Id == additem.Id)
            {
                space += item.GetItemMeta().MaxAmount - item.Amount;
            }

            if (space > additem.Amount) return true;
        }

        return space >= additem.Amount;
    }
}