using System.Collections.Generic;
using horizoncraft.script.Entity;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class ChunkSnapshot
{
    public long Version;
    public int X;
    public int Y;
    public List<BlockSnapshot> list = new();
    public List<Entitydata> Entiydatas = new();
}