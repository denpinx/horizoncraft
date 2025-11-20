using System;
using Godot;
using Horizoncraft.script.Expand;

namespace Horizoncraft.script.WorldControl.Struct.structs;

public class BirchTreeStruct : StructBuild
{
    private BlockMeta _birch_log = Materials.BlockMetas["birch_log"];
    private BlockMeta _birch_leaves = Materials.BlockMetas["birch_leaves"];

    public BirchTreeStruct()
    {
        Name = "birch_tree";
    }

    public override BlockStruct DynamicBuild(int x, int y, int z, Random rand, params object[] args)
    {
        BlockStruct bs = new BlockStruct();

        var pos1 = new Vector2I(x, y) + new Vector2I(rand.Next(-2, 2), rand.Next(-6, -3));
        var pos_l = pos1 + new Vector2I(rand.Next(-5, -2), rand.Next(-5, -3));
        var pos_r = pos1 + new Vector2I(rand.Next(2, 5), rand.Next(-5, -3));

        GenerateLine(bs, x, y, z, pos1.X, pos1.Y, _birch_log);
        GenerateLine(bs, pos1.X, pos1.Y, z, pos_l.X, pos_l.Y, _birch_log);
        GenerateLine(bs, pos1.X, pos1.Y, z, pos_r.X, pos_r.Y, _birch_log);
        MathExpand.ExtendPoints(pos_l, pos_r, 0.4f, out var fpl, out var fpr);
        GenerateLine(bs, (int)fpl.X, (int)fpl.Y, z, (int)fpr.X, (int)fpr.Y, _birch_leaves, true);
        return bs;
    }

    public void GenerateLine(BlockStruct bs, int x, int y, int z, int tox, int toy, BlockMeta blockMeta,
        bool around = false)
    {
        int steps = Math.Max(Math.Abs(tox - x), Math.Abs(toy - y)) + 1;
        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / steps;
            int fx = x + (int)Math.Round((tox - x) * t);
            int fy = y + (int)Math.Round((toy - y) * t);
            if (around)
            {
                bs.AddBlock(fx, fy, z, blockMeta);
                bs.AddBlock(fx + 1, fy, z, blockMeta);
                bs.AddBlock(fx - 1, fy, z, blockMeta);
                bs.AddBlock(fx, fy + 1, z, blockMeta);
                bs.AddBlock(fx, fy - 1, z, blockMeta);
            }
            else
            {
                bs.AddBlock(fx, fy, z, blockMeta);
            }
        }
    }
}