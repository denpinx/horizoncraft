using System;
using System.Collections.Generic;
using horizoncraft.script.Components;
using MemoryPack;

namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class BlockData
    {
        /// <summary>组件属性</summary>
        public List<Component> components = new();

        /// <summary>方块id</summary>
        public int Id;

        /// <summary>方块状态</summary>
        public int State;

        /// <summary>方块光照值</summary>
        public int Light;

        [MemoryPackIgnore] private BlockMeta _blockMeta;
        [MemoryPackIgnore] private Component _lastcomponent;

        [MemoryPackIgnore]
        public BlockMeta BlockMeta
        {
            get
            {
                _blockMeta ??= Materials.Valueof(Id);
                return _blockMeta;
            }
            private set => _blockMeta = value;
        }

        public T GetComponent<T>() where T : Component
        {
            if (_lastcomponent != null && _lastcomponent is T) return _lastcomponent as T;
            var result = components.Find(cmp => cmp is T);
            if (result != null)
            {
                _lastcomponent = result;
                return result as T;
            }

            return null;
        }

        public T GetComponent<T>(string name) where T : Component
        {
            var result = components.Find(cmp => cmp is T && cmp.Name == name);
            if (result != null) return result as T;
            return null;
        }

        public BlockData(BlockMeta meta)
        {
            SetMeta(meta);
        }

        [MemoryPackConstructor]
        public BlockData()
        {
        }

        public void SetMeta(int id) => SetMeta(Materials.Valueof(id));


        public void SetMeta(string name) => SetMeta(Materials.Valueof(name));


        public void SetMeta(BlockMeta meta)
        {
            BlockMeta = meta;
            Id = meta.Id;
            components.Clear();
            foreach (Func<Component> cmp in meta.Components)
                components.Add(cmp());
        }

        public bool IsMeta(string name)
        {
            return BlockMeta.Id == Materials.Valueof(name).Id;
        }

        public BlockTileSet GetBlockTileSet()
        {
            if (BlockMeta.blockTileDatas.Count == 0) return null;
            if (BlockMeta.TileType == "tile")
                return BlockMeta.GetBlockTileSet(State);
            if (BlockMeta.TileType == "atlas")
                return BlockMeta.GetBlockTileSet(0);
            if (BlockMeta.TileType == "terrain")
                return BlockMeta.GetBlockTileSet(State);
            return null;
        }

        public int GetTileSize()
        {
            if (BlockMeta.blockTileDatas.Count == 0) return -1;
            if (BlockMeta.TileType == "tile")
                return BlockMeta.GetBlockTileSet(State).tile_size;
            if (BlockMeta.TileType == "atlas")
                return BlockMeta.GetBlockTileSet(0).tile_count;
            return -1;
        }

        public bool CheckTag(string tagname, string value)
        {
            var v = BlockMeta.GetTag(tagname);
            if (v == null) return false;
            return v == value;
        }

        public string GetTag(string tagname)
        {
            return BlockMeta.GetTag(tagname);
        }

        public void SetLight(int light)
        {
            this.Light = light;
        }
    }
}