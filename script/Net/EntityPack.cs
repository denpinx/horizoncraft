using System.Collections.Generic;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Entity;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class EntityPack
{
    public string From;
    public List<EntityDataSnapShot> Entitys = new();
}