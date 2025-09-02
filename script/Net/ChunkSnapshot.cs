using System.Collections.Generic;
using horizoncraft.script.Components;
using horizoncraft.script.Components.EntityComponents;
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
    public List<EntityData> Entiydatas = new();

    /// <summary>
    /// 重置空的所属权到给予的名称
    /// </summary>
    /// <param name="name">玩家名</param>
    public void ResetEmptyOwned(string name)
    {
        for (int i = 0; i < Entiydatas.Count; i++)
        {
            var entity = Entiydatas[i];
            if (entity.Owned == "")
            {
                entity.Owned = name;
            }
        }
    }
}