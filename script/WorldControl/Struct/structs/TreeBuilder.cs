using System;
using Godot;
using Horizoncraft.script.Expand;

namespace Horizoncraft.script.WorldControl.Struct.structs;

public class TreeBuilder : StructBuild
{
    private BlockMeta _log;
    private BlockMeta _leaves;

    /// <summary>
    /// 主干顶点的X轴偏移
    /// </summary>
    public required int RootDeviate = 2;

    /// <summary>
    /// 主干高度范围严格遵循 X=Nin,Y=Max
    /// </summary>
    public required Vector2I RootHigh = new Vector2I(7, 10);

    /// <summary>
    /// 分支高度范围严格遵循 X=Nin,Y=Max
    /// </summary>
    public required Vector2I PeakHigh = new Vector2I(3, 5);

    /// <summary>
    /// 分支X轴编译范围严格遵循 X=Nin,Y=Max
    /// </summary>
    public required Vector2I PeakDeviate = new Vector2I(2, 5);

    /// <summary>
    /// 顶部树叶的扩展百分比
    /// </summary>
    public required float LeavesExtend = 0.4f;

    public TreeBuilder(string name, string log, string leaves)
    {
        Name = name;
        this._leaves = Materials.BlockMetas[leaves];
        this._log = Materials.BlockMetas[log];
    }

    public override BlockStruct DynamicBuild(int x, int y, int z, Random rand, params object[] args)
    {
        BlockStruct bs = new BlockStruct();

        var pos1 = new Vector2I(x, y) + new Vector2I(rand.Next(-2, 2), rand.Next(-RootHigh.Y, -RootHigh.X));
        var pos_l = pos1 + new Vector2I(rand.Next(-PeakDeviate.Y, -PeakDeviate.X), rand.Next(-PeakHigh.Y, -PeakHigh.X));
        var pos_r = pos1 + new Vector2I(rand.Next(PeakDeviate.X, PeakDeviate.Y), rand.Next(-PeakHigh.Y, -PeakHigh.X));

        GenerateLine(bs, x, y, z, pos1.X, pos1.Y, _log);
        GenerateLine(bs, pos1.X, pos1.Y, z, pos_l.X, pos_l.Y, _log);
        GenerateLine(bs, pos1.X, pos1.Y, z, pos_r.X, pos_r.Y, _log);
        MathExpand.ExtendPoints(pos_l, pos_r, 0.4f, out var fpl, out var fpr);
        GenerateLine(bs, (int)fpl.X, (int)fpl.Y, z, (int)fpr.X, (int)fpr.Y, _leaves, true);
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