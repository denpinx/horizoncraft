using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial struct BlockSnapshot
{
    public byte x;
    public byte y;
    public byte z;
    public short id;
    public byte state;
}

public struct BlockSnapShotItem
{
    public short id;
    public byte state;
}