using System;
using System.Collections.Generic;
using Godot;

namespace horizoncraft.script.Recipes;

public class RecipeManage
{
    private static List<Recipe> Recipes = new List<Recipe>();

    public static void RegRecipe(Recipe recipe)
    {
        if (recipe == null)
        {
            GD.PrintErr($"[RecipeManage] 注册失败! recipe 为 null");
            return;
        }

        var result = Recipes.Find(r => r.Tag == recipe.Tag);
        if (result != null)
        {
            GD.Print($"[RecipeManage] 添加 {recipe.Tag} 配方,{recipe.recipes.Count} 个");
            foreach (var r in recipe.recipes)
                result.recipes.Add(r);
        }
        else
        {
            GD.Print($"[RecipeManage] 创建 {recipe.Tag} 配方,{recipe.recipes.Count} 个");
            Recipes.Add(recipe);
        }
    }

    private static RecipeItem ParseRecipeItem(Dictionary<string, object> dict)
    {
        RecipeItem item = new RecipeItem();
        var CostList = (List<object>)dict["cost"];
        var ResultList = (List<object>)dict["result"];
        if (dict.ContainsKey("process"))
            item.ProcessTick = (int)dict["process"];

        foreach (var costInfo_ in CostList)
        {
            var costInfo = (List<object>)costInfo_;
            var itemstack = Materials.Dictionary_itemmetas[(string)costInfo[0]].GetItemStack();
            itemstack.Amount = (int)costInfo[1];
            item.Cost.Add(itemstack);

            GD.Print($"{itemstack.GetItemMeta().Name},{itemstack.Amount}");
        }

        foreach (var resultInfo_ in ResultList)
        {
            var resultInfo = (List<object>)resultInfo_;
            var itemstack = Materials.Dictionary_itemmetas[(string)resultInfo[0]].GetItemStack();
            itemstack.Amount = (int)resultInfo[1];
            item.Result.Add(itemstack);
        }

        return item;
    }

    private static Recipe ParseFile(string filename)
    {
        var path = "Config/recipes/" + filename;
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"[RecipeManage]] {path} 不存在！");
            return null;
        }

        FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var dict = JsonCleaner.FromJson(fileAccess.GetAsText());
        fileAccess.Close();
        var recipe = new Recipe();
        if (dict.ContainsKey("tag"))
        {
            recipe.Type = Recipe.RecipeType.Precess;
            recipe.Tag = (string)dict["tag"];
        }
        else
        {
            recipe.Type = Recipe.RecipeType.CraftTable;
            recipe.Tag = "Player";
        }

        var list = (List<object>)dict["recipes"];
        foreach (var recipeItem in list)
            recipe.recipes.Add(ParseRecipeItem((Dictionary<string, object>)recipeItem));

        return recipe;
    }

    static RecipeManage()
    {
        if (!DirAccess.DirExistsAbsolute("Config/recipes"))
        {
            GD.PrintErr("[RecipeManage] 初始化失败! 配方目录不存在!");
            return;
        }

        DirAccess dir = DirAccess.Open("Config/recipes");
        var InPathFiles = dir.GetFiles();
        foreach (var filename in InPathFiles)
        {
            if (!filename.EndsWith(".json")) continue;
            var recipe = ParseFile(filename);
            RegRecipe(recipe);
        }
    }


    public static Recipe GetRecipe(string tag)
    {
        var Recipe = Recipes.Find(r => r.Tag == tag);
        if (Recipe == null) GD.PrintErr($"[RecipeManage] 配方{tag} 不存在!");

        return Recipe;
    }

    public static RecipeItem GetRecipeItem(string tag, Func<RecipeItem, bool> mathAction)
    {
        var Recipe = GetRecipe(tag);
        if (Recipe == null) return null;
        foreach (var item in Recipe.recipes)
            if (mathAction(item))
                return item;
        return null;
    }
}