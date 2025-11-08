using MemoryPack;

namespace Horizoncraft.script.Components
{
    [MemoryPackable]
    public partial class TickComponent : Component
    {
        public int Max;
        public int Current;
    }
}