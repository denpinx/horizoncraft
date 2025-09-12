using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Expand;
using MemoryPack;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Entity
{
    [MemoryPackable]
    public partial class EntityData
    {
        [MemoryPackIgnore] public Vector2I Coord => Position.MathFloor(16);
        [MemoryPackIgnore] public Vector2I ChunkCoord => Position.MathFloor(Chunk.Size * 16);
        [MemoryPackIgnore] public Vector2 LastPosition = Vector2.Zero;
        public List<Component> Components = new List<Component>();
        public bool Removed = false;
        public bool Update = true;
        public string Owned = "";
        public string Name = "";
        public Vector2 Position;
        public Guid Uuid;

        public T GetComponent<T>() where T : Component
        {
            var cmp = Components.Find(c => c is T);
            if (cmp != null) return (T)cmp;
            return null;
        }

        public List<T> GetComponents<T>() where T : Component
        {
            return Components.FindAll(c => c is T).Cast<T>().ToList();
        }

        public T GetComponent<T>(string name) where T : Component
        {
            var cmp = Components.Find(c => c is T && name == c.Name);
            if (cmp != null) return (T)cmp;
            return null;
        }
    }
}