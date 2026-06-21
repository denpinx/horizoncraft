using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Utility;

namespace Horizoncraft.script.Inventory;

public class ItemMeta
{
    /// <summary>组件属性集合</summary>
    public List<Func<Component>> Components = new();

    //内部id
    public int Id;
    public string NameSpace = nameof(Horizoncraft);
    public string Name;
    public string Description;
    public bool HasBlock = false;
    public int MaxAmount = 64;
    public ItemStateSet Itemset = new ItemStateSet();
    public Texture2D ShowTexture;
    public BlockMeta BlockMeta = null;
    public Dictionary<string, string> Tags = new Dictionary<string, string>();

    public Texture2D GetTexture(int state = 0)
    {
        if (ShowTexture != null) return ShowTexture;
        if (state > Itemset.Textures.Count)
            GameLogger.Error("Inventory","物品状态贴图遗漏!");
        else return Itemset.Textures[state];
        return null;
    }

    public ItemStack GetItemStack()
    {
        var item = new ItemStack()
        {
            Name = Name,
            Amount = 1,
        };
        foreach (var func in Components)
        {
            var result = func();
            item.Components.Add(result);
        }
        return item;
    }

    public string GetTag(string name)
    {
        if (Tags.ContainsKey(name)) return Tags[name];
        return null;
    }

    public bool AddItemComponentBuildFunc(Func<Component> func)
    {
        //构建-预览
        var result = func();
        if (result is ItemComponent)
        {
            var result_type = result.GetType();
            var itemcmp = Components.Find(cmp => cmp().GetType() == result_type);
            if (itemcmp != null) return false;
            GameLogger.Debug("Inventory", $"方块组件 to 物品组件 {result.Drive} {result}");
            Components.Add(func);
            return true;
        }
        
        return false;
    }
}