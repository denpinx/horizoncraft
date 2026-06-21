using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Utility;

namespace Horizoncraft.script.WorldControl.Struct;

public static class OreManage
{
    public static Dictionary<string, OreConfig> ores = new();

    public static void Registry(OreConfig config)
    {
        GameLogger.Info("Ore",$"[{nameof(OreManage)}] 注册矿脉结构: {config.Name} \t #{ores.Count}");
        ores.Add(config.Name, config);
    }

    private static List<OreConfig> GetOreFliterDeep(int Deep)
    {
        var list = new List<OreConfig>();
        foreach (var item in ores.Values)
        {
            if (item.Deep <= Deep)
                list.Add(item);
        }

        return list;
    }

    /// <summary>
    /// 生成矿物
    /// </summary>
    /// <param name="rand">随机数来源</param>
    /// <param name="x">区块坐标 x</param>
    /// <param name="y">区块坐标 y</param>
    /// <returns>区块内的集合</returns>
    public static BlockStruct GeneratorOre(Random rand, int x, int y)
    {
        //return GeneratorOreV2(rand, x, y);
        Vector2I Gpos = new Vector2I(x * Chunk.Size, y * Chunk.Size);
        BlockStruct blocks = new BlockStruct();
        var configs = GetOreFliterDeep(y);
        foreach (var config in configs)
            for (int i = 0; i < config.Count; i++)
            {
                var offsetpos = new Vector2I(rand.Next(Chunk.Size), rand.Next(Chunk.Size));
                var pos = offsetpos + Gpos;
                if (blocks.HasBlock(pos.X, pos.Y, 1)) continue;
                int trycount = rand.Next(1 + config.Size / 2, config.Size);
                for (int j = 0; j < trycount;)
                {
                    var rand_offset_pos = new Vector2I(rand.Next(-config.Range, config.Range + 1),
                        rand.Next(-config.Range, config.Range + 1));
                    var final_pos = pos + rand_offset_pos;

                    blocks.AddBlock(final_pos.X, final_pos.Y, 1, Materials.Valueof(config.Name));
                    j++;
                }
            }

        return blocks;
    }
}