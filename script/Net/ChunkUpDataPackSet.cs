using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class ChunkUpDataPackSet : AsByteable<ChunkUpDataPackSet>
{
    public List<ChunkUpdataPack> packs = new();
}