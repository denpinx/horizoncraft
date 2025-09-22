using System;
using System.Collections.Generic;
using Godot;
using Microsoft.Win32;

namespace horizoncraft.script.WorldControl.Struct;

public static class OreManage
{
    public static Dictionary<string, OreConfig> ores = new();

    public static void Registry(OreConfig config)
    {
        GD.Print("注册:" + config.Name);
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
        Vector2I Gpos = new Vector2I(x * Chunk.Size, y * Chunk.Size);
        BlockStruct blocks = new BlockStruct();
        var configs = GetOreFliterDeep(y);
        foreach (var config in configs)
            for (int i = 0; i < config.Count; i++)
            {
                var offsetpos = new Vector2I(rand.Next(Chunk.Size), rand.Next(Chunk.Size));
                var pos = offsetpos + Gpos;
                if (blocks.HasBlock(pos.X, pos.Y, 1)) continue;
                for (int j = 0; j < config.Size; j++)
                {
                    var rand_offset_pos = new Vector2I(rand.Next(-config.Size + 1, config.Size),
                        rand.Next(-config.Size + 1, config.Size));
                    var final_pos = pos + rand_offset_pos;
                    blocks.AddBlock(final_pos.X, final_pos.Y, 1, Materials.Valueof(config.Name));
                }
            }

        return blocks;
    }
}