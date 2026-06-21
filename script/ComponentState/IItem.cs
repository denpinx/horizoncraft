using MemoryPack;

namespace Horizoncraft.script.ComponentState;


// [MemoryPackable]
public partial interface IItem
{
    public string Name { get; set; }
    public int Amount { get; set; }
}
