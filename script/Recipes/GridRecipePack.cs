using System.Collections.Generic;
using Godot;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Recipes;

public class GridRecipePack : RecipePack
{
    public List<GridRecipeItem> Recipes = new List<GridRecipeItem>();

    public List<GridRecipeItem> SearchRelated(ItemStack itemStack)
    {
        var list = new List<GridRecipeItem>();
        foreach (var recipe in Recipes)
        {
            if (recipe.Related(itemStack))
            {
                list.Add(recipe);
            }
        }

        return list;
    }
}