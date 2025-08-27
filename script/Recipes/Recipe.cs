using System.Collections.Generic; 

namespace horizoncraft.script.Recipes;

public class Recipe
{
    public enum RecipeType
    {
        CraftTable,
        Precess
    }

    public RecipeType Type { get; set; }
    public string Tag;

    public List<RecipeItem> recipes = new List<RecipeItem>();
}