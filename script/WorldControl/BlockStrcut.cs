using System.Collections.Generic;
using System.Net.Http.Headers;
using Godot;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl
{
    public class BlockStruct
    {
        private Vector2I min = new(0, 0);
        private Vector2I max = new(0, 0);
        public List<BlockStructItem> blockStructItems = new List<BlockStructItem>();
        public void AddBlock(int x, int y, int z, BlockMeta blockMeta, int state)
        {
            if (blockStructItems.Count == 0)
            {
                min = new(x, y);
                max = new(x, y);
            }
            else
            {
                if (x > max.X) max.X = x;
                else min.X = x;
                if (y > max.Y) max.Y = y;
                else min.Y = y;
            }
            blockStructItems.Add(new()
            {
                Coord = new Vector3I(x, y, z),
                BlockMeta = blockMeta,
                State = state
            });
        }
        public (BlockMeta, int) GetBlocMeta(int X, int Y, int Z)
        {
            if (X < min.X || X > max.X || Y < min.Y || Y > max.Y) return (null, 0);
            foreach (BlockStructItem bsi in blockStructItems)
                if (bsi.Coord.X == X && bsi.Coord.Y == Y && bsi.Coord.Z == Z)
                    return (bsi.BlockMeta, bsi.State);
            return (null, 0);
        }
    }
}