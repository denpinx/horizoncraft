using Horizoncraft.script.WorldControl;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class VectorBlockData
{
    public int X;
    public int Y;
    public int Z;
    public BlockData Block;
}