using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Expand;
using Horizoncraft.script.WorldControl;
using Vector3 = System.Numerics.Vector3;

namespace Horizoncraft.script.Events
{
    /// <summary>
    /// [严重]设置方块后不要使用原get的数据操作
    /// </summary>
    public class BlockTickEvent : WorldEvent
    {
        public BlockData BlockData;
        public Chunk Chunk;
        public Vector3I GlobalePos;
        public Vector3I LocalPos;

        private BlockData _bottomBlock;
        private BlockData _topBlock;
        private BlockData _leftBlock;
        private BlockData _rightBlock;
        private BlockData _frontBlock;
        private BlockData _backBlock;

        /// <summary>
        /// 将方块标记为已更新
        /// </summary>
        public void SetUpdate()
        {
            Chunk.UpdateList.Add(LocalPos);
        }

        public void SetBlock(BlockMeta meta, int state = 0) =>
            Chunk.SetBlock(LocalPos.X, LocalPos.Y, LocalPos.Z, meta, state);

        /// <summary>获取下方方块</summary>
        public BlockData GetBottomBlock()
        {
            _bottomBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Up);
            return _bottomBlock;
        }

        public BlockData SetBottomBlock(BlockMeta meta, int state = 0) =>
            _bottomBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Up, meta, state);

        /// <summary>获取上方方块</summary>
        public BlockData GetTopBlock()
        {
            _topBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Down);
            return _topBlock;
        }

        /// <summary>设置顶部方块</summary>
        public BlockData SetTopBlock(BlockMeta meta, int state = 0) =>
            _topBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Down, meta, state);

        /// <summary>获取左边方块</summary>
        public BlockData GetLeftBlock()
        {
            _leftBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Left);
            return _leftBlock;
        }

        /// <summary>设置左边方块</summary>
        public BlockData SetLeftBlock(BlockMeta meta, int state = 0) =>
            _leftBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Left, meta, state);

        /// <summary>获取右边方块</summary>
        public BlockData GetRightBlock()
        {
            _rightBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Right);
            return _rightBlock;
        }

        /// <summary>设置右边方块</summary>
        public BlockData SetRightBlock(BlockMeta meta, int state = 0) =>
            _rightBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Right, meta, state);

        /// <summary>获取前景方块</summary>
        public BlockData GetFrontBlock()
        {
            _frontBlock ??= Service.ChunkService.GetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 1));
            return _frontBlock;
        }

        /// <summary>设置前景方块</summary>
        public BlockData SetFrontBlock(BlockMeta meta, int state = 0) =>
            _frontBlock = Service.ChunkService.SetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 1), meta, state);

        /// <summary>获取背景方块</summary>
        public BlockData GetBackBlock()
        {
            _backBlock ??= Service.ChunkService.GetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 0));
            return _backBlock;
        }

        /// <summary>设置背景方块</summary>
        public BlockData SetBackBlock(BlockMeta meta, int state = 0) =>
            _backBlock = Service.ChunkService.SetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 0), meta, state);


        /// <summary>检查方块是否为指定元数据且不为null</summary>
        public bool CheckMeta(BlockData blockData, BlockMeta meta)
        {
            return blockData != null && blockData.BlockMeta == meta;
        }

        /// <summary>检查方块材质属性是否为Cube且不为null</summary>
        public bool CheckIsCube(BlockData blockData)
        {
            return blockData != null && blockData.BlockMeta.Cube;
        }

        public bool CheckCanReplace(BlockData blockData)
        {
            return blockData != null && blockData.BlockMeta.Replaceable;
        }

        public bool CheckCanReplaceAndNotMeta(BlockData blockData, BlockMeta meta)
        {
            return blockData != null && blockData.BlockMeta.Replaceable && blockData.BlockMeta != meta;
        }

        /// <summary>
        /// 更新周围的所有被动更新方块,注:是延迟到下tick更新
        /// </summary>
        public void UpdateNeighborBlock(bool inside = false,int deep=0)
        {
            World.Service.ChunkService.PassiveUpdateNeighborBlock(GlobalePos,inside,deep);
        }

        public void DropBlockLoot(BlockData blockData)
        {
            blockData?.DropBlockLoot(World, new Vector2I(GlobalePos.X, GlobalePos.Y));
            blockData?.DropBlockInventoryItems(World, new Vector2I(GlobalePos.X, GlobalePos.Y));
        }
        /// <summary>
        /// 检查
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="meta"></param>
        /// <param name="blockData"></param>
        /// <returns></returns>
        public bool CheckOneOfWithOffset(Vector3I[] positions, BlockMeta meta,out BlockData blockData)
        {
            foreach (var pos in positions)
            {
                var block = World.Service.ChunkService.GetBlock(GlobalePos+pos);
                if (block != null && block.BlockMeta == meta)
                {
                    blockData = block;
                    return true;
                }
            }
            blockData = null;
            return false;
        }
        
        public void Reset()
        {
            _bottomBlock = null;
            _topBlock = null;
            _leftBlock = null;
            _rightBlock = null;
            _frontBlock = null;
            _backBlock = null;
        }
    }
}