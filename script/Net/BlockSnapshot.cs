using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial struct BlockSnapshot
{
    public byte x;
    public byte y;
    public byte z;
    public string id;
    public byte state;
}