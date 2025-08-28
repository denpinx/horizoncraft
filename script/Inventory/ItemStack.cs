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

    public void SetItemMeta(string name)
    {
        ItemMeta meta = Materials.Dictionary_itemmetas[name];
        this.Id = meta.Id;
    }

    public ItemMeta GetItemMeta() => Materials.itemmetas[Id];
    public BlockMeta GetBlockMeta() => GetItemMeta().BlockMeta;
    public ItemStack Copy(int amount = 0)
    {
        return new ItemStack()
        {
            Id = this.Id,
            Amount = amount == 0 ? this.Amount : amount,
            State = this.State
        };
    }
}