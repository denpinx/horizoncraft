using MemoryPack;

namespace horizoncraft.script.Components
{
    [MemoryPackable]
    public partial class TickComponent : Component
    {
        public int Max;
        public int Current;
    }
}