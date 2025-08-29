using System;
using System.Linq;
using Godot;

namespace horizoncraft.script.WorldControl.Struct.structs;

public class MegaOakTreeStruct : StructBuild
{
    private BlockMeta _oak_log = Materials.Valueof("oak_log");
    private BlockMeta _oak_leaves = Materials.Valueof("oak_leaves");

    public MegaOakTreeStruct()
    {
        Name = "mega_oak_tree";
    }

    public override BlockStruct DynamicBuild(int x, int y, int z, Random rand)
    {
        var blocks = new BlockStruct();
        Grow(blocks, rand, x, y, z, 25);

        foreach (var bsi in blocks.blockStructItems.Values.ToArray())
        {
            if (bsi.Coord.Y < y - 4)
            {
                if (Mathf.Abs(bsi.Coord.X - x) > 1)
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
        if (w > 20)
        {
            Grow(blocks, rand, x, y - 1, z, w);
        }
        else
        {
            if (rand.Next(25) <= w)
            {
                Grow(blocks, rand, x, y - 1, z, w);
            }
            else
            {
                //分叉
                if (rand.Next(2) == 1)
                {
                    var nowpos = new Vector3(x, y, z);
                    var TargetPos = new Vector3(x - rand.Next(3), y - rand.Next(3), z);
                    //插值
                    for (int i = 0; i < 3; i++)
                    {
                        var lerppos = nowpos.Lerp(TargetPos, 1f / (float)i);
                        blocks.AddBlock((int)lerppos.X, (int)lerppos.Y, z, _oak_log);
                    }

                    Grow(blocks, rand, (int)TargetPos.X, (int)TargetPos.Y, z, w);
                    AddLeaves(blocks, x, y, z);
                }

                if (rand.Next(2) == 1)
                {
                    var nowpos = new Vector3(x, y, z);
                    var TargetPos = new Vector3(x + rand.Next(3), y - rand.Next(3), z);
                    //插值
                    for (int i = 0; i < 3; i++)
                    {
                        var lerppos = nowpos.Lerp(TargetPos, 1f / (float)i);
                        blocks.AddBlock((int)lerppos.X, (int)lerppos.Y, z, _oak_log);
                    }

                    Grow(blocks, rand, (int)TargetPos.X, (int)TargetPos.Y, z, w);
                    AddLeaves(blocks, x, y, z);
                }
            }
        }
    }
}