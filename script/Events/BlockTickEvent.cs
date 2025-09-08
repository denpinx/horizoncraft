using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events
{
    /// <summary>
    /// [严重]设置方块后不要使用原get的数据操作
    /// </summary>
    public class BlockTickEvent : WorldEvent
    {
        public Blockdata Blockdata;
        public Chunk Chunk;
        public Vector3I GlobalePos;
        public Vector3I LocalPos;

        private Blockdata _bottomBlock;
        private Blockdata _topBlock;
        private Blockdata _leftBlock;
        private Blockdata _rightBlock;
        private Blockdata _frontBlock;
        private Blockdata _backBlock;


        public void SetBlock(BlockMeta meta, int state = 0) =>
            Chunk.SetBlock(LocalPos.X, LocalPos.Y, LocalPos.Z, meta, state);

        /// <summary>获取下方方块</summary>
        public Blockdata GetBottomBlock()
        {
            _bottomBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Up);
            return _bottomBlock;
        }

        public Blockdata SetBottomBlock(BlockMeta meta, int state=0) =>
            _bottomBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Up, meta, state);

        /// <summary>获取上方方块</summary>
        public Blockdata GetTopBlock()
        {
            _topBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Down);
            return _topBlock;
        }

        /// <summary>设置顶部方块</summary>
        public Blockdata SetTopBlock(BlockMeta meta, int state=0) =>
            _topBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Down, meta, state);

        /// <summary>获取左边方块</summary>
        public Blockdata GetLeftBlock()
        {
            _leftBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Left);
            return _leftBlock;
        }

        /// <summary>设置左边方块</summary>
        public Blockdata SetLeftBlock(BlockMeta meta, int state=0) =>
            _leftBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Left, meta, state);

        /// <summary>获取右边方块</summary>
        public Blockdata GetRightBlock()
        {
            _rightBlock ??= Service.ChunkService.GetBlock(GlobalePos + Vector3I.Right);
            return _rightBlock;
        }

        /// <summary>设置右边方块</summary>
        public Blockdata SetRightBlock(BlockMeta meta, int state=0) =>
            _rightBlock = Service.ChunkService.SetBlock(GlobalePos + Vector3I.Right, meta, state);

        /// <summary>获取前景方块</summary>
        public Blockdata GetFrontBlock()
        {
            _frontBlock ??= Service.ChunkService.GetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 1));
            return _frontBlock;
        }

        /// <summary>设置前景方块</summary>
        public Blockdata SetFrontBlock(BlockMeta meta, int state=0) =>
            _frontBlock =Service.ChunkService.SetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 1), meta, state);

        /// <summary>获取背景方块</summary>
        public Blockdata GetBackBlock()
        {
            _backBlock ??= Service.ChunkService.GetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 0));
            return _backBlock;
        }

        /// <summary>设置背景方块</summary>
        public Blockdata SetBackBlock(BlockMeta meta, int state=0) =>
            _backBlock = Service.ChunkService.SetBlock(new Vector3I(GlobalePos.X, GlobalePos.Y, 0), meta, state);


        /// <summary>检查方块是否为指定元数据且不为null</summary>
        public bool CheckMeta(Blockdata blockdata, BlockMeta meta)
        {
            return blockdata != null && blockdata.BlockMeta == meta;
        }

        /// <summary>检查方块材质属性是否为Cube且不为null</summary>
        public bool CheckIsCube(Blockdata blockdata)
        {
            return blockdata != null && blockdata.BlockMeta.Cube;
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