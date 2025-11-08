using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class WorldSnapshot
{
    public List<ChunkSnapshot> chunks = new();
}