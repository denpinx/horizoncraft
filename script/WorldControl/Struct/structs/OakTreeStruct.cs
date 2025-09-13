using System;
using System.ComponentModel;
using System.Linq;

namespace horizoncraft.script.WorldControl.Struct.structs;

public class OakTreeStruct : StructBuild
{
    private BlockMeta _oak_log = Materials.Valueof("oak_log");
    private BlockMeta _oak_leaves = Materials.Valueof("oak_leaves");

    public OakTreeStruct()
    {
        Name = "oak_tree";
    }

    public override BlockStruct DynamicBuild(int x, int y, int z, Random rand,params object[] args)
    {
        var blocks = new BlockStruct();
        Grow(blocks, rand, x, y, z, 10);

        foreach (var bsi in blocks.blockStructItems.Values.ToArray())
        {
            if (bsi.Coord.Y < y - 4)
            {
                AddLeaves(blocks, bsi.Coord.X, bsi.Coord.Y, bsi.Coord.Z);
            }
        }

        return blocks;
    }

    public void AddLeaves(BlockStruct blocks, int x, int y, int z)
    {
        blocks.AddBlock(x + 1, y - 1, z, _oak_leaves);
        blocks.AddBlock(x - 1, y - 1, z, _oak_leaves);
        blocks.AddBlock(x + 1, y, z, _oak_leaves);
        blocks.AddBlock(x - 1, y, z, _oak_leaves);
        blocks.AddBlock(x, y - 1, z, _oak_leaves);
    }

    public void Grow(BlockStruct blocks, Random rand, int x, int y, int z, int w)
    {
        if (w <= 0) return;
        blocks.AddBlock(x, y, z, _oak_log);
        w--;
        //越往上分支越多
        if (w > 5)
        {
            Grow(blocks, rand, x, y - 1, z, w);
        }
        else
        {
            if (rand.Next(10) <= w)
            {
                Grow(blocks, rand, x, y - 1, z, w);
            }
            else
            {
                //分叉
                if (rand.Next(2) == 1)
                {
                    Grow(blocks, rand, x - 1, y - 1, z, w);
                }

                if (rand.Next(2) == 1)
                {
                    Grow(blocks, rand, x + 1, y - 1, z, w);
                }
            }
        }
    }
}