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
    public int Size = 3 * 3;

    public List<RecipeItem> recipes = new List<RecipeItem>();
}