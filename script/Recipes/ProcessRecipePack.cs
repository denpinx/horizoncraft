using System.Collections.Generic;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Recipes;

/// <summary>
/// 处理配方
/// </summary>
public class ProcessRecipePack : RecipePack
{
    //标签
    public List<ProcessRecipeItem> Recipes = new();
    public List<ProcessRecipeItem> SearchRelated(ItemStack itemStack)
    {
        var list = new List<ProcessRecipeItem>();
        foreach (var recipe in Recipes)
        {
            foreach (var item in recipe.Cost)
            {
                if (item == null) continue;

                if (recipe.MatchType == RecipeItemMatchType.ItemMatch)
                {
                    if (item.Name == itemStack.Name)
                    {
                        list.Add(recipe);
                        continue;
                    }
                }   

                if (recipe.MatchType == RecipeItemMatchType.TagMatch)
                {
                    if (item.Name == itemStack.GetItemMeta().GetTag("thesaurus"))
                    {
                        list.Add(recipe);
                    }
                }
            }
        }

        return list;
    }
}