using MemoryPack;

namespace horizoncraft.script.Components;

[MemoryPackable]
public partial class ItemComponent:Component
{
    public int Amount;
    public int MaxAmount;
}