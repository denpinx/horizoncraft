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
using horizoncraft.script.Inventory;
using MemoryPack;

namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class Blockdata
    {
        public List<Component> components = new();
        public int ID;
        public int STATE = 0;
        
        public int Light = 0;

        [MemoryPackIgnore] private BlockMeta _blockMeta;

        [MemoryPackIgnore]
        public BlockMeta BlockMeta
        {
            get
            {
                if (_blockMeta == null)
                {
                    _blockMeta = Materials.Valueof(ID);
                }

                return _blockMeta;
            }
            set { _blockMeta = value; }
        }

        public T GetComponent<T>() where T : Component
        {
            for (int i = 0; i < components.Count; i++)
                if (components[i] is T t)
                    return t;
            return null;
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
                components.Add(cmp());
        }

        public bool IsMeta(String ID)
        {
            return BlockMeta.ID == Materials.Valueof(ID).ID;
        }

        public BlockTileSet GetBlockTileSet()
        {
            if (BlockMeta.blockTileDatas.Count == 0) return null;
            if (BlockMeta.Tiletype == "tile")
                return BlockMeta.GetBlockTileSet(STATE);
            if (BlockMeta.Tiletype == "atlas")
                return BlockMeta.GetBlockTileSet(0);
            if (BlockMeta.Tiletype == "terrain")
                return BlockMeta.GetBlockTileSet(STATE);
            return null;
        }

        public int GetTileSize()
        {
            if (BlockMeta.blockTileDatas.Count == 0) return -1;
            if (BlockMeta.Tiletype == "tile")
                return BlockMeta.GetBlockTileSet(STATE).tile_size;
            if (BlockMeta.Tiletype == "atlas")
                return BlockMeta.GetBlockTileSet(0).tile_count;
            return -1;
        }

        public bool CheckTag(string tagname, string value)
        {
            var v = BlockMeta.GetTag(tagname);
            if (v == null) return false;
            if (v == value) return true;
            return false;
        }

        public void SetLight(int light)
        {
            this.Light = light;
        }
    }
}