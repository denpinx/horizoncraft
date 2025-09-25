using System.Collections.Generic;
using horizoncraft.script.Inventory;
using YamlDotNet.Core.Tokens;

namespace horizoncraft.script.Recipes;

public class ProcessRecipeItem
{
    public RecipeItemMatchType MatchType = RecipeItemMatchType.ItemMatch;
    public int ProcessTick = 0;
    public Dictionary<string,string> CostTagMatch = new();
    public List<ItemStack> Cost = new();
    public List<ItemStack> Result = new();
    /// <summary>
    /// 匹配物品是否满足标签
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool ItemTagMatch(ItemStack[] items)
    {
        foreach (var item in items)
        foreach (var tag in CostTagMatch)
        {
            if (item == null) return false;
            if (item.GetItemMeta().GetTag(tag.Key) != tag.Value) return false;
        }
        return true;
    }
    public bool ItemMatch(ItemStack[] items)
    {
        if (MatchType == RecipeItemMatchType.TagMatch)
            return ItemTagMatch(items);
        
        if (items.Length == 0) return false;
        foreach (var item in items)
        foreach (var cost in Cost)
        {
            if (item == null) return false;
            if(item.Id!=cost.Id&&item.Amount!=cost.Amount)return false;
        }
        return true;
    }
}