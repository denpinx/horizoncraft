using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class PlayerPack
{
    public List<PlayerDataSnapshot> players = new();
}