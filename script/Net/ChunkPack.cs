using System;
using System.Collections.Generic;
using Horizoncraft.script.WorldControl;
using MemoryPack;

namespace Horizoncraft.script.Net;

/// <summary>
/// 全量同步
/// </summary>
[MemoryPackable]
public partial class ChunkPack
{
    //要同步的id
    public List<Chunk> Chunks = new List<Chunk>();

    public HashSet<Guid> GetAllEntitys()
    {
        HashSet<Guid> ret = new HashSet<Guid>();
        foreach (var chunk in Chunks)
        {
            foreach (var entity in chunk.Entitys)
            {
                ret.Add(entity.Uuid);
            }
        }

        return ret;
    }
}