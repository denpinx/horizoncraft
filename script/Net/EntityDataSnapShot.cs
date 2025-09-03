using Godot;
using horizoncraft.script.WorldControl;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class EntityDataSnapShot
{
    public string Owned;
    public string Uuid;
    public Vector2 Position;
    
    [MemoryPackIgnore]
    public Vector2I Coord
    {
        get { return World.MathFloor(new Vector2I((int)Position.X, (int)Position.Y), 16); }
    }

    [MemoryPackIgnore]
    public Vector2I ChunkCoord
    {
        get { return World.MathFloor(new Vector2I((int)Position.X, (int)Position.Y), Chunk.Size * 16); }
    }
}