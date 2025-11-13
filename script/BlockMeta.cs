using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Events;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.WorldControl;
using Horizoncraft.script.WorldControl.Struct;
using Dictionary = System.Collections.Generic.Dictionary<string, string>;

namespace Horizoncraft.script
{
    /// <summary>
    /// 方块配置
    /// </summary>
    public class BlockMeta
    {
        public bool TileVisible = true;
        /// <summary>组件属性集合</summary>
        public List<Func<Component>> Components = [];

        public List<int> RenderSystem = [];

        public List<Component> Examples = [];
        //如果有多张不同状态的贴图，这个只会获取第一张
        public Texture2D Texture;
        // TODO 待实现,或将转移指组件system中执行
        /// <summary>方块放置事件</summary>
        public Action<PlaceBlockEvent> PlaceBlockEvent;

        // TODO 待实现,或将转移指组件system中执行
        /// <summary>方块破坏事件</summary>
        public Action<BlockTickEvent> BlockTickEvent;

        /// <summary>决定不同方块状态的不同tile</summary>
        public List<BlockTileSet> blockTileDatas = new();

        public List<OverCollideSet> overCollideDatas = new();

        /// <summary>其他扩展属性和标签</summary>
        public Dictionary Tags = new();

        /// <summary>Tile贴图的类型</summary>
        public string TileType = "tile";

        /// <summary>方块名 "xxx_xxx"全小写 + 下划线格式</summary>
        public string Name;

        /// <summary>唯一ID，注册时分配</summary>
        public int Id;

        /// <summary>是否有碰撞</summary>
        public bool Collide = true;

        /// <summary>是否为完整方块</summary>
        public bool Cube = true;

        /// <summary>是否可以被直接替换</summary>
        public bool Replaceable = false;

        /// <summary>是否为光照方块</summary>
        public bool Light = false;

        /// <summary>硬度，决定破坏时间</summary>
        public float Rigidity = 0.5f;

        public int BreakLevel = 0;

        public bool IsLiquid = false;

        public OreConfig OreConfig = null;

        /// <summary>掉落物</summary>
        public LootTable LootTable;

        // TODO 待实现
        /// <summary>方块的大小,这里不是指像素大小，而是一个方块究竟占多少格子，如果1*1就占1个格子，如果是2*2就占4个格子</summary>
        public Vector2I GridSize;

        /// <summary>对应的物品配置 </summary>
        public ItemMeta ItemMeta = null;

        /// <summary>物品输入遮罩 </summary>
        public HashSet<int> InputMask = new();

        /// <summary>物品输出遮罩 </summary>
        public HashSet<int> OutputMask = new();


        //
        public List<LootItemSnapshot> _LootItemSnapshots_ = new();


        /// <summary>获取方块实列</summary>
        public BlockData CreateBlockData()
        {
            return new BlockData(this);
        }

        /// <summary>
        /// 获取方块的Tile配置
        /// </summary>
        /// <param name="state">当前方块的状态</param>
        /// <returns>当前状态对应的配置</returns>
        public BlockTileSet GetBlockTileSet(int state)
        {
            var result = blockTileDatas.Find(blocktileset => blocktileset.state == state);
            if (result != null)
                return result;

            if (blockTileDatas.Count == 1)
                return blockTileDatas[0];
            return null;
        }

        /// <summary>
        /// 获取标签
        /// </summary>
        /// <param name="name">标签名</param>
        /// <returns>标签值，如果标签名不存在则返回null</returns>
        public string GetTag(string name)
        {
            string str = "";
            if (Tags.TryGetValue(name, out str))
                return str;
            return null;
        }

        public bool HasComponent<T>()
        {
            foreach (var component in Examples)
                if (component is T)
                    return true;
            return false;
        }
    }
}