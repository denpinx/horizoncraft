using System.Collections.Generic;

namespace horizoncraft.script.Recipes;

/// <summary>
/// 处理配方
/// </summary>
public class ProcessRecipePack
{
    //标签
    public string Tag;
    public List<ProcessRecipeItem> Recipes = new();
}