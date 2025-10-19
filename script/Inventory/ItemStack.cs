using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Interfaces;
using horizoncraft.script.Entity;
using horizoncraft.script.Net;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
public partial class ItemStack
{
    public string Name;
    public int Amount;
    public int State;
    public List<Component> Components = new();

    public ItemMeta GetItemMeta() => Materials.ItemMetas[Name];
    public BlockMeta GetBlockMeta() => GetItemMeta().BlockMeta;

    //拷贝
    public ItemStack Copy(int amount = 0)
    {
        var item = GetItemMeta().GetItemStack();
        item.Amount = amount == 0 ? Amount : amount;
        item.State = State;
        //todo 组件是否应该支持运行时状态复制？还是不应该复制组件？直接重构？
        //item.Components.Clear();
        // foreach (var component in Components)
        //     if (component is ICopy copy)
        //         item.Components.Add(copy.Copy());
        return item;
    }

    public T GetComponent<T>() where T : Component
    {
        foreach (var cmp in Components)
            if (cmp is T t)
                return t;
        return null;
    }

    public T GetComponent<T>(string name) where T : Component
    {
        foreach (var cmp in Components)
            if (cmp is T t && t.Name == name)
                return t;
        return null;
    }

    public ItemStack TryStackItem(ItemStack item)
    {
        if (item.Name != Name) return item;
        int space = GetItemMeta().MaxAmount - Amount;
        if (space >= item.Amount)
        {
            Amount += item.Amount;
            item.Amount = 0;
            return item;
        }
        else
        {
            Amount += space;
            item.Amount -= space;
            return item;
        }
    }


    public EntityData GetEntityData(Vector2I position)
    {
        var data = new EntityData()
        {
            Name = "item_entity",
            Owned = PlayerNode.Profile.Name,
            Position = new Vector2(position.X, position.Y),
            Components = new List<Component>()
            {
                new ItemEntityComponent()
                {
                    Name = "ItemEntityComponent",
                    ItemStack = this
                }
            }
        };
        return data;
    }
}