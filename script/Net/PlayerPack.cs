using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class PlayerPack
{
    public List<PlayerdataSnapshot> players = new();
}