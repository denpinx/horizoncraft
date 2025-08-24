using System.Collections.Generic;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Recipes;

public class RecipeItem
{
    public int ProcessTick = 0;
    public List<ItemStack> Cost = new();
    public List<ItemStack> Result = new();
}