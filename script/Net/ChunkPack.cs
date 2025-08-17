using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using horizoncraft.script.WorldControl;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class ChunkPack : AsByteable<ChunkPack>
{
    //要同步的id
    public List<Chunk> Chunks = new List<Chunk>();
}