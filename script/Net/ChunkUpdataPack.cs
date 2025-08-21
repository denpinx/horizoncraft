using System.Collections.Generic;
using System.Numerics;
using MemoryPack;

namespace horizoncraft.script.Net;

/// <summary>
/// 增量同步
/// </summary>
[MemoryPackable]
public partial class ChunkUpdataPack
{
    public int x;
    public int y;
    public List<BlockSnapshot> updates = new();
}