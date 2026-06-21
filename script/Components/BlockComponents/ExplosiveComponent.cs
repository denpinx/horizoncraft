using MemoryPack;
using Horizoncraft.script.Components.BlockComponents;

namespace Horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class ExplosiveComponent : TickComponent
{
    public int Power = 3;
    public int FuseTime = 80;
    public int Fuse = 0;
}
