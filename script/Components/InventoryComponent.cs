using System.Collections.Generic;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Recipes;
using MemoryPack;

namespace Horizoncraft.script.Components;

[MemoryPackable]
public partial class InventoryComponent : TickComponent
{
    public string InventoryName = "";
    public string InventoryTile = "容器";
    public int Size = 36;

    public BlockInventory Inventory;

    public BlockInventory GetInventory()
    {
        if (Inventory == null) Inventory = new BlockInventory(Size);
        return Inventory;
    }

    public virtual bool Checkfield(string name)
    {
        return false;
    }

    public bool MathRecipe(ItemStack recipeItem, int formindex, int targetindex)
    {
        var formitem = GetInventory().GetItem(formindex);
        return (formitem != null &&
                recipeItem != null &&
                formitem.Name == recipeItem.Name &&
                formitem.Amount >= recipeItem.Amount
            );
    }

    public (int, ItemStack) TryTakeItem(BlockMeta meta, int amount, bool reduce = true)
    {
        for (int i = 0; i < Size; i++)
        {
            if (meta.OutputMask.Count > 0 && !meta.OutputMask.Contains(i)) continue;
            var item = GetInventory().GetItem(i);
            if (item != null)
            {
                if (reduce)
                {
                    GetInventory().ReduceItemAmount(i);
                }

                return (i, item.Copy(amount));
            }
        }

        return (0, null);
    }

    private bool HasSpace(BlockMeta meta, ItemStack additem, bool input)
    {
        int space = 0;
        for (int i = 0; i < Size; i++)
        {
            if (input)
            {
                if (meta.InputMask.Count > 0 && !meta.InputMask.Contains(i)) continue;
            }
            else if (meta.OutputMask.Count > 0 && !meta.OutputMask.Contains(i)) continue;

            ItemStack item = GetInventory().GetItem(i);
            if (item == null)
                return true;
            if (item.Name == additem.Name)
            {
                space += item.GetItemMeta().MaxAmount - item.Amount;
            }

            if (space > additem.Amount) return true;
        }

        return space >= additem.Amount;
    }

    public bool TryPushItem(BlockMeta meta, ItemStack additem)
    {
        if (!HasSpace(meta, additem, true)) return false;

        for (int i = 0; i < Size; i++)
        {
            if (meta.InputMask.Count > 0 && !meta.InputMask.Contains(i)) continue;
            ItemStack item = GetInventory().GetItem(i);
            if (item == null)
            {
                GetInventory().SetItem(i, additem);
                GetInventory().update = true;
                GetInventory().OnItemAdd?.Invoke(i, additem);
                return true;
            }
            else if (item.Name == additem.Name)
            {
                int max = item.GetItemMeta().MaxAmount;
                int space = max - item.Amount;
                //空间足够 
                if (space > 0 && space >= additem.Amount)
                {
                    item.Amount += additem.Amount;
                    additem.Amount = 0;
                    GetInventory().update = true;
                    GetInventory().OnItemAdd?.Invoke(i, additem);
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
}