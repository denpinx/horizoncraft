using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.Components;
using MemoryPack;

namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class Blockdata
    {
        public List<Component> components = new();
        public int ID;
        public int STATE = 0;

        [MemoryPackIgnore] private BlockMeta _blockMeta;

        [MemoryPackIgnore]
        public BlockMeta BlockMeta
        {
            get
            {
                if (_blockMeta == null)
                {
                    // o(1)
                    _blockMeta = Materials.Valueof(ID);
                }

                return _blockMeta;
            }
            set { _blockMeta = value; }
        }

        public T GetComponent<T>(string name) where T : Component
        {
            for (int i = 0; i < components.Count; i++)
                if (components[i].Name == name)
                    return (T)components[i];
            return null;
        }

        public Blockdata(BlockMeta meta)
        {
            SetMeta(meta);
        }

        [MemoryPackConstructor]
        public Blockdata()
        {
        }

        public void SetMeta(int id)
        {
            SetMeta(Materials.Valueof(id));
        }

        public void SetMeta(string name)
        {
            SetMeta(Materials.Valueof(name));
        }

        public void SetMeta(BlockMeta meta)
        {
            this.BlockMeta = meta;
            this.ID = meta.ID;
            components.Clear();
            foreach (Func<Component> cmp in meta.Components)
                components.Add(cmp.Invoke());
        }

        public bool IsMeta(String ID)
        {
            BlockMeta = Materials.Valueof(this.ID);
            return BlockMeta.ID == Materials.Valueof(ID).ID;
        }

        public int GetTileId()
        {
            BlockMeta = Materials.Valueof(this.ID);
            if (BlockMeta.blockTileDatas.Count == 0) return -1;
            if (BlockMeta.Tiletype == "tile")
                return BlockMeta.GetBlockTileSet(STATE).tile_id;
            if (BlockMeta.Tiletype == "atlas")
                return BlockMeta.GetBlockTileSet(0).tile_id;
            return -1;
        }

        public int GetTileSize()
        {
            BlockMeta = Materials.Valueof(this.ID);
            if (BlockMeta.blockTileDatas.Count == 0) return -1;
            if (BlockMeta.Tiletype == "tile")
                return BlockMeta.GetBlockTileSet(STATE).tile_size;
            if (BlockMeta.Tiletype == "atlas")
                return BlockMeta.GetBlockTileSet(0).tile_count;
            return -1;
        }
    }
}