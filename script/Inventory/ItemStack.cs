using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Net;
using MemoryPack;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
public partial class ItemStack
{
    public int Id;
    public int Amount;
    public int State;
    public List<Component> Components = new();
    
    public ItemMeta GetItemMeta() => Materials.ItemMetas[Id];
    public BlockMeta GetBlockMeta() => GetItemMeta().BlockMeta;

    //深拷贝
    public ItemStack Copy(int amount = 0)
    {
        var item = GetItemMeta().GetItemStack();
        item.Amount = amount==0?Amount:amount;
        item.State = State;
        item.Components.Clear();
        foreach (var component in this.Components)
            item.Components.Add(((ItemComponent)component).Copy());
        return item;
    }

    public T GetComponent<T>() where T : Component
    {
        var result = Components.Find(cmp => cmp is T);
        if (result != null) return result as T;
        return null;
    }

    public T GetComponent<T>(string name) where T : Component
    {
        var result = Components.Find(cmp => cmp is T && cmp.Name == name);
        if (result != null) return result as T;
        return null;
    }
}