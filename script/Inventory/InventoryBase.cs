using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Net;
using MemoryPack;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
[MemoryPackUnion(0, typeof(PlayerInventory))]
[MemoryPackUnion(1, typeof(BlockInventory))]
public abstract partial class InventoryBase
{
    [MemoryPackIgnore] public Action<int, int> OnAddItemAmount;
    [MemoryPackIgnore] public Action<int, int> OnReduceItemAmount;
    [MemoryPackIgnore] public Action<int, ItemStack> OnItemAdd;

    [MemoryPackIgnore] public bool update = true;
    public int Size = 0;
    public ItemStack[] Items;

    public InventoryBase(int size)
    {
        this.Size = size;
        Items = new ItemStack[Size];
    }

    /// <summary>
    /// 获取空余的物品栏下标
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public int GetEmpyIndex(int start = 0)
    {
        for (int i = start; i < Size; i++)
        {
            if (GetItem(i) == null) return i;
        }

        return -1;
    }

    /// <summary>
    /// 获取物品
    /// </summary>
    /// <param name="index">下标</param>
    /// <returns></returns>
    public ItemStack GetItem(int index)
    {
        if (Items[index] != null && Items[index].Amount <= 0)
            Items[index] = null;
        return Items[index];
    }

    /// <summary>
    /// 设置物品
    /// </summary>
    /// <param name="index">下标</param>
    /// <param name="item">物品</param>
    public void SetItem(int index, ItemStack item)
    {
        if (item == null || item.Amount <= 0)
            Items[index] = null;
        else
            Items[index] = item;
        update = true;
    }

    /// <summary>
    /// 减少物品数量，如果物品堆叠数小于等于0则被删除
    /// </summary>
    /// <param name="id">物品下标</param>
    /// <param name="amount">减少数量</param>
    public void ReduceItemAmount(int id, int amount = 1)
    {
        if (Items[id] == null)
            return;

        Items[id].Amount -= amount;
        if (Items[id].Amount <= 0) Items[id] = null;

        OnReduceItemAmount?.Invoke(id, amount);
    }

    /// <summary>
    /// 添加物品数量,返回剩余无法添加的数量
    /// </summary>
    /// <param name="id">物品下标</param>
    /// <param name="amount">数量</param>
    /// <returns>剩余无法添加的数量</returns>
    public int AddItemAmount(int id, int amount = 1)
    {
        if (Items[id] == null)
        {
            GD.PrintErr("Inventory: SubItemAmount: Item not found!");
            return amount;
        }

        Items[id].Amount += amount;
        int les = Items[id].Amount - Items[id].GetItemMeta().MaxAmount;
        if (les > 0)
            return les;
        OnAddItemAmount?.Invoke(id, amount);
        return 0;
    }

    /// <summary>
    /// 是否为全空
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        for (int i = 0; i < Size; i++)
        {
            var item = GetItem(i);
            if (item != null) return false;
        }

        return true;
    }

    /// <summary>
    /// 该下标的物品是否为空
    /// </summary>
    /// <param name="index">下标</param>
    /// <returns></returns>
    public bool IndexIsEmpty(int index)
    {
        if (Items[index] != null && Items[index].Amount <= 0)
            Items[index] = null;
        return Items[index] == null;
    }

    /// <summary>
    /// 尝试添加该物品
    /// </summary>
    /// <param name="additem"></param>
    /// <returns>如果物品栏如果无法添加该物品，则返回false</returns>
    public bool TryAddItem(ItemStack additem)
    {
        if (!HasSpace(additem)) return false;
        for (int i = 0; i < Size; i++)
        {
            ItemStack item = GetItem(i);
            if (item == null)
            {
                SetItem(i, additem);
                update = true;
                OnItemAdd?.Invoke(i, additem);
                return true;
            }
            else if (item.Id == additem.Id)
            {
                int max = item.GetItemMeta().MaxAmount;
                int space = max - item.Amount;
                //空间足够 
                if (space > 0 && space >= additem.Amount)
                {
                    item.Amount += additem.Amount;
                    additem.Amount = 0;
                    update = true;
                    OnItemAdd?.Invoke(i, additem);
                    return true;
                }

                //空间不足
                if (space > 0 && space < additem.Amount)
                {
                    item.Amount = max;
                    additem.Amount -= space;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 是否有空间可以添加物品
    /// </summary>
    /// <param name="additem"></param>
    /// <returns></returns>
    private bool HasSpace(ItemStack additem)
    {
        int space = 0;
        for (int i = 0; i < Size; i++)
        {
            ItemStack item = GetItem(i);
            if (item == null)
                return true;
            if (item.Id == additem.Id)
            {
                space += item.GetItemMeta().MaxAmount - item.Amount;
            }

            if (space > additem.Amount) return true;
        }

        return space >= additem.Amount;
    }


    /// <summary>
    /// 排序
    /// </summary>
    public void Sort()
    {
        List<ItemStack> ItemStacks = new();
        for (int Index = 0; Index < Size; Index++)
        {
            ItemStack item = GetItem(Index);
            if (item != null)
            {
                for (var resultIndex = 0; resultIndex < ItemStacks.Count; resultIndex++)
                {
                    if (ItemStacks[resultIndex].Id == item.Id)
                    {
                        var space = ItemStacks[resultIndex].GetItemMeta().MaxAmount - ItemStacks[resultIndex].Amount;
                        if (space > 0)
                        {
                            if (space < item.Amount)
                            {
                                ItemStacks[resultIndex].Amount += space;
                                item.Amount -= space;
                            }

                            if (space >= item.Amount)
                            {
                                ItemStacks[resultIndex].Amount += item.Amount;
                                item.Amount = 0;
                                break;
                            }
                        }
                    }
                }

                if (item.Amount > 0)
                    ItemStacks.Add(item);
            }
        }

        ItemStacks.Sort((a, b) => a.Id.CompareTo(b.Id));
        for (int i = 0; i < Size; i++)
        {
            if (i < ItemStacks.Count)
            {
                Items[i] = ItemStacks[i];
            }
            else
            {
                Items[i] = null;
            }
        }

        update = true;
    }

    /// <summary>
    /// 清空物品栏
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < Size; i++)
            SetItem(i, null);
    }
}