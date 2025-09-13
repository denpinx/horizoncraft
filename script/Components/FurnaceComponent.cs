using System.Collections.Generic;
using horizoncraft.script.Components.Interfaces;
using horizoncraft.script.Inventory;
using MemoryPack;

namespace horizoncraft.script.Components;

[MemoryPackable]
public partial class FurnaceComponent : InventoryComponent, IGetProgress
{
    public int FuelMax = 0;
    public int Fuel = 0;
    public int ProcessTick = 0;
    public int Progress = 0;
    public List<ItemStack> Result;

    public ProgressValue GetProgress()
    {
        if (Progress > 0)
            return new ProgressValue
            {
                Name = "冶炼",
                Max = ProcessTick,
                Value = Progress
            };
        else
            return new ProgressValue
            {
                Name = "",
                Max = ProcessTick,
                Value = Progress
            };
    }
}