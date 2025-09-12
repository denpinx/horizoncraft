using System;
using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class UUIDPack
{
    public List<Guid> uuids = new();
}