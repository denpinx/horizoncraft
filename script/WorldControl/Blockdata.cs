using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;
using MemoryPack;

namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class BlockData
    {
        /// <summary>组件属性</summary>
        public List<Component> Components = new();

        /// <summary>方块id</summary>
        public string Id;

        /// <summary>方块状态</summary>
        public int State;

        /// <summary>方块光照值</summary>
        [MemoryPackIgnore] public int Light;

        [MemoryPackIgnore] public int OldLight=0;

        [MemoryPackIgnore] private BlockMeta _blockMeta;

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
            var result = Components.Find(cmp => cmp is T);
            return result as T;
        }

        public bool HasComponent<T>() where T : Component
        {
            return Components.Find(cmp => cmp is T) != null;
        }

        public T GetComponent<T>(string name) where T : Component
        {
            var result = Components.Find(cmp => cmp is T && cmp.Name == name);
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
        public void SetMeta(string name) => SetMeta(Materials.Valueof(name));


        public void SetMeta(BlockMeta meta)
        {
            BlockMeta = meta;
            Id = meta.Name;
            Components.Clear();
            foreach (Func<Component> cmp in meta.Components)
                Components.Add(cmp());
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

        /// <summary>
        /// 掉落方块的掉落物
        /// </summary>
        /// <param name="World"></param>
        /// <param name="GlobalePos"></param>
        public void DropBlockLoot(World World, Vector2I GlobalePos)
        {
            if (BlockMeta.LootTable != null)
            {
                var items = BlockMeta.LootTable.TryTakeItem(State);
                foreach (var item in items)
                {
                    World.Service.EntityService.AddEntityData(
                        item.GetEntityData(new Vector2I(GlobalePos.X * 16, GlobalePos.Y * 16)));
                }
            }
        }
        /// <summary>
        /// 掉落方块内的容器物品
        /// </summary>
        /// <param name="World"></param>
        /// <param name="GlobalePos"></param>
        public void DropBlockInventoryItems(World World, Vector2I GlobalePos)
        {
            //掉落容器物品
            foreach (var cmp in Components)
            {
                if (cmp is InventoryComponent inv)
                {
                    foreach (var item in inv.GetInventory().Items)
                    {
                        if (item != null)
                        {
                            World.Service.EntityService.AddEntityData(
                                item.GetEntityData(new Vector2I(GlobalePos.X * 16, GlobalePos.Y * 16)));
                        }
                    }
                }
            }
        }
    }
}