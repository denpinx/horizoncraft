using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class EntityPack
{
    public string From;
    public List<EntityDataSnapShot> Entitys = new();
}