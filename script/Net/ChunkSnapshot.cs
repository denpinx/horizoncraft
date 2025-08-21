using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class ChunkSnapshot
{
    public long version;
    public int x;
    public int y;
    public List<BlockSnapshot> list = new List<BlockSnapshot>();
}