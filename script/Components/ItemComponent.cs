using MemoryPack;

namespace horizoncraft.script.Components;

[MemoryPackable]
public partial class ItemComponent : Component
{
    public virtual ItemComponent Copy()
    {
        return new ItemComponent()
        {
            Name = this.Name,
        };
    }
}