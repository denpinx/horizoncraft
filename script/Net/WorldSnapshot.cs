using System.Collections.Concurrent;
using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class WorldSnapshot
{
    public List<ChunkSnapshot> chunks = new();
}