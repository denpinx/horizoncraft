using System;
using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class UUIDPack
{
    public List<Guid> uuids = new();
}