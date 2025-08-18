using System.Collections.Generic;
using System.Numerics;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class ChunkUpdataPack : AsByteable<ChunkUpdataPack>
{
    public int x;
    public int y;
    public List<BlockSnapshot> updates = new();
}