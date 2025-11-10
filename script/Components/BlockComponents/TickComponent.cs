using Horizoncraft.script.Components;
using MemoryPack;

namespace Horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class TickComponent : Component
{
    public int Max;
    public int Current;
}