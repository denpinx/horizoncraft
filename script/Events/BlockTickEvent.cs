using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events
{
    public class BlockTickEvent : WorldEvent
    {
        public Blockdata Blockdata;

        public Vector3I GloablPos;
        public Vector3I LocalPos;
        public BlockMeta SetBlockMeta
        {
            set { WorldService.SetBlock(LocalPos, value); }
        }
        /// <summary>获取下方方块</summary>
        public Blockdata BottomBlock
        {
            get { return WorldService.GetBlock(GloablPos + Vector3I.Up); }
        }

        /// <summary>设置下方方块</summary>
        public BlockMeta BottomBlockMeta
        {
            set { WorldService.SetBlock(GloablPos + Vector3I.Up, value); }
        }

        /// <summary>获取上方方块</summary>
        public Blockdata TopBlock
        {
            get { return WorldService.GetBlock(GloablPos + Vector3I.Down); }
        }

        /// <summary>设置上方方块</summary>
        public BlockMeta TopBlockMeta
        {
            set { WorldService.SetBlock(GloablPos + Vector3I.Down, value); }
        }

        /// <summary>获取左边方块</summary>
        public Blockdata LeftBlock
        {
            get { return WorldService.GetBlock(GloablPos + Vector3I.Left); }
        }

        /// <summary>设置左边方块</summary>
        public BlockMeta LeftBlockMeta
        {
            set { WorldService.SetBlock(GloablPos + Vector3I.Left, value); }
        }

        /// <summary>获取右边方块</summary>
        public Blockdata RightBlock
        {
            get { return WorldService.GetBlock(GloablPos + Vector3I.Right); }
        }

        /// <summary>设置右边方块</summary>
        public BlockMeta RightBlockMeta
        {
            set { WorldService.SetBlock(GloablPos + Vector3I.Right, value); }
        }

        /// <summary>获取前景方块</summary>
        public Blockdata FontBlock
        {
            get { return WorldService.GetBlock(new(GloablPos.X, GloablPos.Y, 1)); }
        }

        /// <summary>设置前景方块</summary>
        public BlockMeta FontBlockmeta
        {
            set { WorldService.SetBlock(new(GloablPos.X, GloablPos.Y, 1), value); }
        }

        /// <summary>获取背景方块</summary>
        public Blockdata BackBlock
        {
            get { return WorldService.GetBlock(new(GloablPos.X, GloablPos.Y, 0)); }
        }

        /// <summary>设置背景方块</summary>
        public BlockMeta BackBlockmeta
        {
            set { WorldService.SetBlock(new(GloablPos.X, GloablPos.Y, 0), value); }
        }
        /// <summary>检查方块是否为指定材质且不为null,成功则执行action</summary>
        public bool CheckMeta(Blockdata blockdata, BlockMeta meta)
        {
            if (blockdata != null && blockdata.BlockMeta == meta)
            {
                return true;
            }
            return false;
        }
        /// <summary>检查方块材质属性是否为Cube且不为null,成功则执行action</summary>
        public bool CheckIsCube(Blockdata blockdata)
        {
            if (blockdata != null && blockdata.BlockMeta.CUBE)
            {
                return true;
            }
            return false;
        }
    }
}
