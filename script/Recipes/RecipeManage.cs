using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Recipes;

public class RecipeManage
{
    private static List<Recipe> ProcessRecipes = new List<Recipe>();
    private static List<GridRecipe> GridRecipes = new List<GridRecipe>();


    public static GridRecipeItem MatchGridRecipe(ItemStack[,] items, string tag)
    {
        var gr = GridRecipes.Find(gr => gr.Tag == tag);
        if (gr == null) return null;
        var gri = gr.recipes.Find(gri => gri.Match(items));
        return gri;
    }
    public static GridRecipeItem GetRecipe(InventoryBase inventory, int RecipeSize, int start_index = 0)
    {
        ItemStack[,] PrimeItems = new ItemStack[RecipeSize, RecipeSize];
        for (int x = 0; x < RecipeSize; x++)
        {
            for (int y = 0; y < RecipeSize; y++)
            {
                int index = start_index + y * RecipeSize + x;
                PrimeItems[x, y] = inventory.GetItem(index);
            }
        }

        //压缩网格配方大小用于匹配配方
        Vector2I min = new Vector2I(RecipeSize, RecipeSize), max = new Vector2I(0, 0);
        for (int x = 0; x < RecipeSize; x++)
        {
            for (int y = 0; y < RecipeSize; y++)
                if (PrimeItems[x, y] != null)
                {
                    if (x < min.X) min.X = x;
                    if (y < min.Y) min.Y = y;
                    if (x > max.X) max.X = x;
                    if (y > max.Y) max.Y = y;
                }
        }

        int wide = max.X - min.X + 1;
        int height = max.Y - min.Y + 1;
        if (wide <= 0 || height <= 0)
            return null;

        ItemStack[,] ResultItems = new ItemStack[wide, height];
        for (int x = 0; x < wide; x++)
        {
            for (int y = 0; y < height; y++)
            {
                ResultItems[x, y] = PrimeItems[min.X + x, min.Y + y];
            }
        }

        return RecipeManage.MatchGridRecipe(ResultItems, "player");
    }

    public static void RegGridRecipe(GridRecipe recipe)
    {
        if (recipe == null)
        {
            GD.PrintErr($"[RecipeManage] 注册失败! recipe 为 null");
            return;
        }

        var result = GridRecipes.Find(r => r.Tag == recipe.Tag);
        if (result != null)
        {
            GD.Print($"[RecipeManage] 添加 {recipe.Tag} 配方,{recipe.recipes.Count} 个");
            foreach (var r in recipe.recipes)
                result.recipes.Add(r);
        }
        else
        {
            GD.Print($"[RecipeManage] 创建 {recipe.Tag} 配方,{recipe.recipes.Count} 个");
            GridRecipes.Add(recipe);
        }
    }

    public static void RegRecipe(Recipe recipe)
    {
        if (recipe == null)
        {
            GD.PrintErr($"[RecipeManage] 注册失败! recipe 为 null");
            return;
        }

        var result = ProcessRecipes.Find(r => r.Tag == recipe.Tag);
        if (result != null)
        {
            GD.Print($"[RecipeManage] 添加 {recipe.Tag} 配方,{recipe.recipes.Count} 个");
            foreach (var r in recipe.recipes)
                result.recipes.Add(r);
        }
        else
        {
            GD.Print($"[RecipeManage] 创建 {recipe.Tag} 配方,{recipe.recipes.Count} 个");
            ProcessRecipes.Add(recipe);
        }
    }

    private static GridRecipeItem ParseGridRecipeItem(Dictionary<string, object> dict)
    {
        GridRecipeItem item = new GridRecipeItem();
        var cost_list = (List<object>)dict["cost"];
        var Mask_ = (Dictionary<string, object>)dict["mask"];
        var result_list = (List<object>)dict["result"];

        var result_count = 1;
        var result_name = (string)result_list[0];
        if (result_list.Count > 1) result_count = (int)result_list[1];

        Dictionary<string, string> mask = new();
        foreach (var mask_item in Mask_)
            mask.Add(mask_item.Key, (string)mask_item.Value);

        int maxy = cost_list.Count;
        int max = ((string)cost_list[0]).Length;
        List<string> strlist = new List<string>();
        foreach (var r in cost_list)
            strlist.Add((string)r);

        ItemStack[,] cost = new ItemStack[max, maxy];
        for (int x = 0; x < max; x++)
        {
            for (int y = 0; y < maxy; y++)
            {
                GD.Print($"{x},{y} at {strlist[y][x]} ");

                var key = strlist[y][x];
                var itemname = mask[key.ToString()];
                if (itemname == "air") cost[x, y] = null;
                else
                    cost[x, y] = Materials.Dictionary_itemmetas[itemname].GetItemStack();
            }
        }

        item.Cost = cost;
        item.Result = Materials.Dictionary_itemmetas[result_name].GetItemStack();
        item.Result.Amount = result_count;
        return item;
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

    private static GridRecipe ParseGridFile(Dictionary<string, object> dict)
    {
        var recipe = new GridRecipe();
        if (dict.ContainsKey("tag"))
        {
            recipe.Tag = (string)dict["tag"];
        }

        var list = (List<object>)dict["recipes"];
        foreach (var recipeItem in list)
            recipe.recipes.Add(ParseGridRecipeItem((Dictionary<string, object>)recipeItem));

        return recipe;
    }

    private static Recipe ParseProcessRecipeFile(Dictionary<string, object> dict)
    {
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

    private static void ParseFile(string filename)
    {
        var path = "Config/recipes/" + filename;
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"[RecipeManage]] {path} 不存在！");
            return;
        }

        FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var dict = JsonCleaner.FromJson(fileAccess.GetAsText());
        fileAccess.Close();

        if (dict.ContainsKey("type"))
        {
            if (dict["type"].ToString() == "process")
            {
                var recipe = ParseProcessRecipeFile(dict);
                RegRecipe(recipe);
            }

            if (dict["type"].ToString() == "craft")
            {
                var recipe = ParseGridFile(dict);
                RegGridRecipe(recipe);
            }
        }

        return;
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
            ParseFile(filename);
        }
    }


    public static Recipe GetRecipe(string tag)
    {
        var Recipe = ProcessRecipes.Find(r => r.Tag == tag);
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