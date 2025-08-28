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

    public int GetEmpyIndex()
    {
        for (int i = 0; i < Size; i++)
        {
            if (GetItem(i) == null) return i;
        }

        return -1;
    }

    public ItemStack GetItem(int id)
    {
        if (Items[id] != null && Items[id].Amount <= 0)
            Items[id] = null;
        return Items[id];
    }

    public void SetItem(int id, ItemStack item)
    {
        if (item == null || item.Amount <= 0)
            Items[id] = null;
        else
            Items[id] = item;
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

    public bool IsLoaded()
    {
        return false;
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < Size; i++)
        {
            var item = GetItem(i);
            if (item != null) return false;
        }

        return true;
    }

    public bool IsEmpty(int id)
    {
        if (Items[id] != null && Items[id].Amount <= 0)
            Items[id] = null;
        return Items[id] == null;
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
    /// 自动排序
    /// </summary>
    public void Sort()
    {
        //压缩空间
        for (int i = Size - 1; i > 0; i--)
        {
            ItemStack item = GetItem(i);
            if (item != null)
            {
                for (int j = 0; j < Size; j++)
                {
                    ItemStack target = GetItem(j);
                    if (target == null)
                    {
                        SetItem(j, item.Copy());
                        SetItem(i, null);
                        break;
                    }
                }
            }
        }

        update = true;
        //排序
    }

    public void Clear()
    {
        for (int i = 0; i < Size; i++)
            SetItem(i, null);
    }

    public ItemStack TryTakeItem(int amout)
    {
        for (int i = 0; i < Size; i++)
        {
            var item = GetItem(i);
            if (item != null)
            {
                ReduceItemAmount(i);
                return item.Copy(1);
            }
        }

        return null;
    }
}