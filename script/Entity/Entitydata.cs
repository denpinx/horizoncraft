using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
using System.Threading.Tasks;
using Godot;
using MemoryPack;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Entity
{
    [MemoryPackable]
    public partial class Entitydata
    {
        public Vector2 position;
        public int id;

        [MemoryPackIgnore]
        public Vector2I Coord { get { return World.MathFloor(new Vector2I((int)position.X, (int)position.Y), 16); } }
        [MemoryPackIgnore]
        public Vector2I ChunkCoord { get { return World.MathFloor(new Vector2I((int)position.X, (int)position.Y), Chunk.Size * 16); } }
    }
}
