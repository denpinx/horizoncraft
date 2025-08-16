using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.WorldControl;
using MemoryPack;
using Vector2 = Godot.Vector2;

namespace horizoncraft.script
{
    [MemoryPackable]
    public partial class PlayerData
    {
        public int PeerId;
        public String Name;
        public Vector2 Position;

        [MemoryPackIgnore] public Player player;

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

        public PlayerData()
        {
        }
    }
}