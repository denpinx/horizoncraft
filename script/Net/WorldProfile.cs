using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class WorldProfile
{
    public long Time;
    public string WorldName;
    public string CreateDate;
    public string LoadDate;
    public long WorldSeed;
}