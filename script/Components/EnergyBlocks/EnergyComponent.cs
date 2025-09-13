using horizoncraft.script.Components.Interfaces;
using MemoryPack;

namespace horizoncraft.script.Components.EnergyBlocks;

[MemoryPackable]
public partial class EnergyUnitComponent : TickComponent, IGetProgress
{
    public int EnergyUnitMax;
    public int EnergyUnitValue;
    public int Rate = 0;
    public bool InputAble = true;
    
    public ProgressValue GetProgress()
    {
        return new ProgressValue
        {
            Name = "EU",
            Max = EnergyUnitMax,
            Value = EnergyUnitValue
        };
    }
}