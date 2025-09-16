using System;
using Godot;
using horizoncraft.script.Entity;
using horizoncraft.script.Expand;
using horizoncraft.script.WorldControl;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class EntityDataSnapShot
{
    public string Owned;
    public Guid Uuid;
    public Vector2 Position;

    [MemoryPackConstructor]
    public EntityDataSnapShot()
    {
        
    }
    public EntityDataSnapShot(EntityData entity)
    {
        Owned = entity.Owned;
        Uuid = entity.Uuid;
        Position = entity.Position;
    }
    [MemoryPackIgnore] public Vector2I Coord => Position.MathFloor(16);
    [MemoryPackIgnore] public Vector2I ChunkCoord => Position.MathFloor(Chunk.Size * 16);
}