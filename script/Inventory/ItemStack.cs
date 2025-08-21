using System.Collections.Generic;
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

    public BlockMeta GetBlockMeta() => Materials.Valueof(GetItemMeta().Name);
}