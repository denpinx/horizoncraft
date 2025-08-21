using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;

namespace horizoncraft.script.Inventory;

public class ItemMeta
{
    public int Id;
    public string Name;
    public string Description;
    public bool HasBlock = false;
    public int MaxAmount = 64;
    public ItemStateSet Itemset = new ItemStateSet();
    
    public Texture2D GetTexture(int state)
    {
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
        return item;
    }
}