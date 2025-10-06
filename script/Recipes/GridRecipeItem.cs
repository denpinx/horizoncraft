using Godot;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Recipes;

public class GridRecipeItem
{
    public RecipeItemMatchType MatchType = RecipeItemMatchType.ItemMatch;
    public string[,] CostTagMatch;
    public ItemStack[,] Cost;
    public ItemStack Result;

    public bool Match(ItemStack[,] items)
    {
        if (MatchType == RecipeItemMatchType.TagMatch)
            return ItemTagMatch(items);


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
                if (a != null && b != null && a.Name != b.Name) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 匹配物品是否满足标签
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool ItemTagMatch(ItemStack[,] items)
    {
        if (items.GetLength(1) != CostTagMatch.GetLength(1) ||
            items.GetLength(0) != CostTagMatch.GetLength(0)
           )
            return false;

        for (int i = 0; i < CostTagMatch.GetLength(0); i++)
        {
            for (int j = 0; j < CostTagMatch.GetLength(1); j++)
            {
                var a = CostTagMatch[i, j];
                var b = items[i, j];
                //如果两个当中有一个不为null则不匹配
                if ((a == null || b == null) && (a != null || b != null)) return false;
                if (a != null && b.GetItemMeta().GetTag("thesaurus") != a)
                    return false;
            }
        }

        return true;
    }

    public bool Related(ItemStack itemStack)
    {
        if (MatchType == RecipeItemMatchType.ItemMatch)
        {
            foreach (var cost in Cost)
            {
                if (cost != null && cost.Name == itemStack.Name)
                {
                    return true;
                }
            }
        }

        if (MatchType == RecipeItemMatchType.TagMatch)
        {
            foreach (var str in CostTagMatch)
            {
                if (str == itemStack.GetItemMeta().GetTag("thesaurus"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}