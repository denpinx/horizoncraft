using System.Linq;
using Horizoncraft.script.Components.Interfaces;
using Horizoncraft.script.ComponentState;
using MemoryPack;

namespace Horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class TankComponent : ReactiveComponent, IStorage<Fluid>,IGetProgress
{
    public Storage<Fluid> FluidStorage = new Storage<Fluid>().AddState(new State<Fluid>()
    {
        FilterMode = StateFilter.Any,
        LogisticsMode = LogisticsMode.Any,
        Value = new Fluid(),
        Max = 10000,
    });

    public Storage<Fluid> GetStorage()
    {
        return FluidStorage;
    }

    public ProgressValue GetProgress()
    {
        var state = FluidStorage.States.First();
        return new ProgressValue()
        {
            Name = state.Value.Name,
            Max = state.Max,
            Value = state.Value.Amount
        };
    }
}