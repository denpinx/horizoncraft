using Godot;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Recipes;

public class GridRecipeItem
{
    public ItemStack[,] Cost;
    public ItemStack Result;

    public bool Match(ItemStack[,] items)
    {
        if (items.GetLength(1) != Cost.GetLength(1) ||
            items.GetLength(0) != Cost.GetLength(0)
           )
            return false;

        for (int i = 0; i < Cost.GetLength(0); i++)
        {
            for (int j = 0; j < Cost.GetLength(1); j++)
            {
                var a = Cost[i, j];
                var b = items[i, j];
                //如果两个当中有一个不为null则不匹配
                if ((a == null || b == null) && (a != null || b != null)) return false;
                if (a != null && b != null && a.Id != b.Id) return false;
            }
        }

        return true;
    }
}