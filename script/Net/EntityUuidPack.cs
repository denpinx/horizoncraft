using System;
using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class EntityUuidPack
{
    public HashSet<Guid> Uuids = new HashSet<Guid>();

    public override int GetHashCode()
    {
        return Uuids.GetHashCode();
    }

    public bool Equals(EntityUuidPack pack)
    {
        return pack.Uuids.SetEquals(Uuids);
    }
}