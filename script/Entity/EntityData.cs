using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Components;
using MemoryPack;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Entity
{
    [MemoryPackable]
    public partial class EntityData
    {
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

        public string Name = "";
        public string Owned = "";
        public string Uuid = "";
        public Vector2 Position;
        public bool Update = true;
        public int Id;
        public List<Component> Components = new List<Component>();

        public T GetComponent<T>() where T : Component
        {
            var cmp = Components.Find(c => c is T);
            if (cmp != null) return (T)cmp;
            return null;
        }

        public T GetComponent<T>(string name) where T : Component
        {
            var cmp = Components.Find(c => c is T && name == c.Name);
            if (cmp != null) return (T)cmp;
            return null;
        }
    }
}