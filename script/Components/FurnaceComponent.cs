using System.Collections.Generic;
using horizoncraft.script.Inventory;
using MemoryPack;

namespace horizoncraft.script.Components;

[MemoryPackable]
public partial class FurnaceComponent : InventoryComponent
{
    public int FuelMax = 0;
    public int Fuel = 0;
    public int ProcessTick = 0;
    public int Progress = 0;
    public List<ItemStack> Result;
}