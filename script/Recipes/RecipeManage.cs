using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Utility;
using FileAccess = Godot.FileAccess;

namespace Horizoncraft.script.Recipes;

/// <summary>
/// 支持把不同目录的json文件，的相同配方类型给自动合并，一个json既支持只写一个，多个json也可以对同一个类型添加
/// 本质上每个json文件都是对某个配方类型的追加，不会重复。
/// </summary>
[Obsolete("该类型已弃用，请使用NeoRecipeManage",true)]
public static class RecipeManage
{
    private static readonly List<ProcessRecipePack> ProcessRecipes = new List<ProcessRecipePack>();
    private static readonly List<GridRecipePack> GridRecipes = new List<GridRecipePack>();


    public static GridRecipeItem MatchGridRecipe(ItemStack[,] items, string tag)
    {
        var gr = GridRecipes.Find(gr => gr.Tag == tag);
        if (gr == null) return null;
        var gri = gr.Recipes.Find(gri => gri.Match(items));
        return gri;
    }

    public static GridRecipeItem GetRecipe(InventoryBase inventory, int RecipeSize, int start_index = 0,
        string tag = "workbench")
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

        return RecipeManage.MatchGridRecipe(ResultItems, tag);
    }

    public static void RegGridRecipe(GridRecipePack recipePack)
    {
        if (recipePack == null)
        {
            GameLogger.Error("RecipeManage",$"[{nameof(RecipeManage)}] 注册失败,配方异常为null。");
            return;
        }

        var result = GridRecipes.Find(r => r.Tag == recipePack.Tag);
        if (result != null)
        {
            GameLogger.Info("RecipeManage", $"添加配方项 {recipePack.Tag,-16} #{result.Recipes.Count,-4} + {recipePack.Recipes.Count,-4}");
            foreach (var r in recipePack.Recipes)
                result.Recipes.Add(r);
        }
        else
        {
            GameLogger.Info("RecipeManage", $"创建配方组 {recipePack.Tag,-16} #{recipePack.Recipes.Count,-4}");
            GridRecipes.Add(recipePack);
        }
    }

    public static void RegRecipe(ProcessRecipePack processRecipePack)
    {
        if (processRecipePack == null)
        {
            GameLogger.Error("RecipeManage",$"[RecipeManage] 注册失败! recipe 为 null");
            return;
        }

        var result = ProcessRecipes.Find(r => r.Tag == processRecipePack.Tag);
        if (result != null)
        {
            GameLogger.Info("RecipeManage", $"添加 {processRecipePack.Tag} 配方,{processRecipePack.Recipes.Count} 个");
            foreach (var r in processRecipePack.Recipes)
                result.Recipes.Add(r);
        }
        else
        {
            GameLogger.Info("RecipeManage", $"创建 {processRecipePack.Tag} 配方,{processRecipePack.Recipes.Count} 个");
            ProcessRecipes.Add(processRecipePack);
        }
    }

    private static GridRecipeItem ParseGridRecipeItem(Dictionary<string, object> dict)
    {
        GridRecipeItem item = new GridRecipeItem();


        List<object> cost_list = (List<object>)dict["template"];
        Dictionary<string, object> Mask_;

        if (dict.TryGetValue("mask-tag", out var value))
        {
            item.MatchType = RecipeItemMatchType.TagMatch;
            Mask_ = (Dictionary<string, object>)value;
        }
        else
        {
            Mask_ = (Dictionary<string, object>)dict["mask"];
        }

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
        string[,] strs = new string[max, maxy];
        for (int x = 0; x < max; x++)
        {
            for (int y = 0; y < maxy; y++)
            {
                if (item.MatchType == RecipeItemMatchType.TagMatch)
                {
                    var key = strlist[y][x];
                    var tag = mask[key.ToString()];
                    if (tag == "air") strs[x, y] = null;
                    else
                    {
                        strs[x, y] = tag;
                    }
                }

                if (item.MatchType == RecipeItemMatchType.ItemMatch)
                {
                    var key = strlist[y][x];
                    var itemname = mask[key.ToString()];
                    if (itemname == "air") cost[x, y] = null;
                    else
                        cost[x, y] = Materials.ItemMetas[itemname].GetItemStack();
                }
            }
        }

        item.CostTagMatch = strs;
        item.Cost = cost;
        item.Result = Materials.ItemMetas[result_name].GetItemStack();
        item.Result.Amount = result_count;
        return item;
    }

    private static ProcessRecipeItem ParseRecipeItem(Dictionary<string, object> dict)
    {
        ProcessRecipeItem item = new ProcessRecipeItem();

        List<object> costList;
        if (dict.TryGetValue("extended-tag", out var value))
        {
            item.ExtendedTag = (Dictionary<string, object>)value;
        }

        if (dict.ContainsKey("cost-tag"))
        {
            item.MatchType = RecipeItemMatchType.TagMatch;
            costList = (List<object>)dict["cost-tag"];
        }
        else
            costList = (List<object>)dict["cost"];


        var ResultList = (List<object>)dict["result"];
        if (dict.ContainsKey("process"))
            item.ProcessTick = (int)dict["process"];

        foreach (var costInfo_ in costList)
        {
            var costInfo = (List<object>)costInfo_;
            var itemstack = Materials.ItemMetas[(string)costInfo[0]].GetItemStack();
            itemstack.Amount = (int)costInfo[1];
            item.Cost.Add(itemstack);
        }

        foreach (var resultInfo_ in ResultList)
        {
            var resultInfo = (List<object>)resultInfo_;
            var itemstack = Materials.ItemMetas[(string)resultInfo[0]].GetItemStack();
            itemstack.Amount = (int)resultInfo[1];
            item.Result.Add(itemstack);
        }

        return item;
    }

    private static GridRecipePack ParseGridFile(Dictionary<string, object> dict)
    {
        var recipe = new GridRecipePack();
        if (dict.TryGetValue("tag", out object value))
            recipe.Tag = (string)value;


        var list = (List<object>)dict["recipes"];
        foreach (var recipeItem in list)
            recipe.Recipes.Add(ParseGridRecipeItem((Dictionary<string, object>)recipeItem));

        return recipe;
    }

    private static ProcessRecipePack ParseProcessRecipeFile(Dictionary<string, object> dict)
    {
        var recipe = new ProcessRecipePack();
        if (dict.TryGetValue("tag", out var value))
            recipe.Tag = (string)value;


        var list = (List<object>)dict["recipes"];
        foreach (var recipeItem in list)
            recipe.Recipes.Add(ParseRecipeItem((Dictionary<string, object>)recipeItem));

        return recipe;
    }

    private static void ParseFile(string filename)
    {
        var path = filename;
        if (!FileAccess.FileExists(path))
        {
            GameLogger.Error("RecipeManage",$"[RecipeManage]] {path} 不存在！");
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
        if (!DirAccess.DirExistsAbsolute("res://config/recipes"))
        {
            GameLogger.Error("RecipeManage","[RecipeManage] 初始化失败! 配方目录不存在!");
            return;
        }

        var list = new List<string>();
        DirUtility.GetFiles("res://config/recipes",".json", list);
        foreach (var file in list)
            ParseFile(file);
    }


    public static ProcessRecipePack GetRecipe(string tag)
    {
        var recipe = ProcessRecipes.Find(r => r.Tag == tag);
        if (recipe == null) GameLogger.Error("RecipeManage",$"[RecipeManage] 配方{tag} 不存在!");
        return recipe;
    }

    public static ProcessRecipeItem GetProcessRecipe(string tag, Func<ProcessRecipeItem, bool> mathAction)
    {
        var recipe = GetRecipe(tag);
        if (recipe == null) return null;
        var result = recipe.Recipes.Find(rcp => mathAction(rcp));
        return result;
    }

    /// <summary>
    /// 获取匹配的物品处理配方
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="Items"></param>
    /// <returns></returns>
    public static ProcessRecipeItem GetProcessRecipe(string tag, ItemStack[] Items)
    {
        var recipe = GetRecipe(tag);
        if (recipe == null) return null;
        var result = recipe.Recipes.Find(r => r.ItemMatch(Items));
        return result;
    }
    /// <summary>
    /// 搜寻物品参与合成的配方
    /// </summary>
    /// <param name="itemStack"></param>
    /// <returns></returns>
    public static Dictionary<string, RecipePack> SearchRecipeByUsefor(ItemStack itemStack)
    {
        Dictionary<string, RecipePack> result = new();

        foreach (var recipe in GridRecipes)
        {
            var searchRelated = recipe.SearchRelated(itemStack);
            if (searchRelated.Count > 0)
            {
                var pack = new GridRecipePack();
                pack.Tag = recipe.Tag;
                pack.Recipes = searchRelated;
                result.Add(pack.Tag, pack);
            }
        }

        foreach (var pack in ProcessRecipes)
        {
            var list = pack.SearchRelated(itemStack);
            if (list.Count > 0)
            {
                ProcessRecipePack gridPack = new();
                gridPack.Tag = pack.Tag;
                gridPack.Recipes = list;
                result.Add(gridPack.Tag, gridPack);
            }
        }

        GameLogger.Debug("RecipeManage", $"搜索物品作用配方 {itemStack.Name} 结果 {result.Count} 个");
        return result;
    }

    /// <summary>
    /// 搜寻物品来源
    /// </summary>
    /// <param name="itemStack"></param>
    /// <returns></returns>
    public static Dictionary<string, RecipePack> SearchRecipeBySource(ItemStack itemStack)
    {
        Dictionary<string, RecipePack> result = new();
        foreach (var pack in GridRecipes)
        {
            GridRecipePack gridPack = new();
            gridPack.Tag = pack.Tag;
            foreach (var recipe in pack.Recipes)
            {
                var item = recipe.Result;
                if (recipe.MatchType == RecipeItemMatchType.ItemMatch)
                {
                    if (item.Name == itemStack.Name)
                    {
                        gridPack.Recipes.Add(recipe);
                        continue;
                    }
                }

                if (recipe.MatchType == RecipeItemMatchType.TagMatch)
                {
                    if (item.Name == itemStack.GetItemMeta().GetTag("thesaurus"))
                    {
                        gridPack.Recipes.Add(recipe);
                        continue;
                    }
                }
            }

            if (gridPack.Recipes.Count > 0)
                result.Add(pack.Tag, gridPack);
        }

        foreach (var pack in ProcessRecipes)
        {
            ProcessRecipePack gridPack = new();
            gridPack.Tag = pack.Tag;
            foreach (var recipe in pack.Recipes)
            {
                foreach (var item in recipe.Result)
                {
                    if (item == null) continue;

                    if (recipe.MatchType == RecipeItemMatchType.ItemMatch)
                    {
                        if (item.Name == itemStack.Name)
                        {
                            gridPack.Recipes.Add(recipe);
                            continue;
                        }
                    }

                    if (recipe.MatchType == RecipeItemMatchType.TagMatch)
                    {
                        if (item.Name == itemStack.GetItemMeta().GetTag("thesaurus"))
                        {
                            gridPack.Recipes.Add(recipe);
                            continue;
                        }
                    }
                }
            }

            if (gridPack.Recipes.Count > 0)
                result.Add(gridPack.Tag, gridPack);
        }

        GameLogger.Debug("RecipeManage", $"搜索物品来源配方 {itemStack.Name} 结果 {result.Count} 个");
        return result;
    }
}