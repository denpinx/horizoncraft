using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;

namespace horizoncraft.script.Inventory;

public class ItemMeta
{
    /// <summary>组件属性集合</summary>
    public List<Func<Component>> Components = new();

    public int Id;
    public string Name;
    public string Description;
    public bool HasBlock = false;
    public int MaxAmount = 64;
    public ItemStateSet Itemset = new ItemStateSet();
    public Texture2D ShowTexture;
    public BlockMeta BlockMeta = null;
    public Dictionary<string, string> Tags = new Dictionary<string, string>();

    public Texture2D GetTexture(int state=0)
    {
        if (ShowTexture != null) return ShowTexture;
        if (state > Itemset.Textures.Count)
            GD.PrintErr("物品状态贴图遗漏!");
        else return Itemset.Textures[state];
        return null;
    }

    public ItemStack GetItemStack()
    {
        var item = new ItemStack()
        {
            Id = Id,
            Amount = 1,
        };
        foreach (var component in Components)
            item.Components.Add(component());
        return item;
    }

    public string GetTag(string name)
    {
        if (Tags.ContainsKey(name)) return Tags[name];
        return null;
    }
}