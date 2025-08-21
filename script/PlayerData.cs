using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;
using Vector2I = Godot.Vector2I;

namespace horizoncraft.script
{
    [MemoryPackable]
    public partial class PlayerData
    {
        public int PeerId;
        public String Name;
        public Vector2 Position;
        public PlayerInventory Inventory = new();

        [MemoryPackIgnore] public Player player;

        [MemoryPackIgnore]
        public Vector2I Coord
        {
            get { return World.MathFloor(new Vector2I((int)Position.X, (int)Position.Y), 16); }
        }

        public Vector2I Position_v2i
        {
            get { return new Vector2I((int)Position.X, (int)Position.Y); }
        }

        public Godot.Vector2 Position_v2
        {
            get { return new Godot.Vector2((int)Position.X, (int)Position.Y); }
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