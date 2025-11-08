using System.Collections.Generic;
using Godot;

namespace Horizoncraft.script.WorldControl
{
    public class BlockStruct
    {
        private Vector2I min = new(0, 0);

        private Vector2I max = new(0, 0);

        //public List<BlockStructItem> blockStructItems = new();
        public Dictionary<Vector3I, BlockStructItem> blockStructItems = new();

        public bool HasBlock(int x, int y, int z)
        {
            return blockStructItems.ContainsKey(new Vector3I(x, y, z));
        }

        public void AddBlock(int x, int y, int z, BlockMeta blockMeta, int state = 0)
        {
            if (HasBlock(x, y, z)) return;
            if (blockStructItems.Count == 0)
            {
                min = new(x, y);
                max = new(x, y);
            }
            else
            {
                if (x > max.X) max.X = x;
                if (x < min.X) min.X = x;
                if (y > max.Y) max.Y = y;
                if (y < min.Y) min.Y = y;
            }

            var pos = new Vector3I(x, y, z);
            blockStructItems.Add(pos, new()
            {
                Coord = pos,
                BlockMeta = blockMeta,
                State = state
            });
        }

        public virtual (BlockMeta, int) GetBlockMeta(int X, int Y, int Z)
        {
            if (X < min.X || X > max.X || Y < min.Y || Y > max.Y) return (null, 0);
            var pos = new Vector3I(X, Y, Z);
            if (blockStructItems.ContainsKey(pos))
            {
                return (blockStructItems[pos].BlockMeta, blockStructItems[pos].State);
            }
            else
            {
                return (null, 0);
            }
        }
    }
}