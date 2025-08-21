using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using horizoncraft.script.WorldControl;
using MemoryPack;

namespace horizoncraft.script.Net;
/// <summary>
/// 全量同步
/// </summary>
[MemoryPackable]
public partial class ChunkPack
{
    //要同步的id
    public List<Chunk> Chunks = new List<Chunk>();
}