using System.Collections.Generic;
using horizoncraft.script.Inventory;
using horizoncraft.script.Recipes;
using MemoryPack;

namespace horizoncraft.script.Components;

[MemoryPackable]
public partial class InventoryComponent : Component
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
                formitem.Id == recipeItem.Id &&
                formitem.Id >= recipeItem.Id
            );
    }
}