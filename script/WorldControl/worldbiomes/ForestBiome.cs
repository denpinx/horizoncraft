using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class ForestBiome : LandBiome
    {
        public ForestBiome()
        {
            name = "森林";
            weight = 3;
            GetHigh = (noise, x, z) => ((int)(noise.GetNoise2D(x * Chunk.Size, z) * 64)) - new Random(HashCode.Combine(x, z)).Next(8);
            GeneratorStruct = (noise, random, structs, gx, gy, z) =>
            {
                if (random.Next(7) != 1) return;
                BlockStruct blockStrcut = new BlockStruct();
                SetBlockWork sbw = blockStrcut.work;
                if (gy < 0)
                    for (int h = 0; h < 5 + random.Next(4); h++)
                    {
                        sbw.ExclList.Add(new(new(gx, gy - h, z), Materials.Valueof("oak_log"), 0));
                        //随机分支
                        if (random.Next(4) == 1)
                        {
                            sbw.ExclList.Add(new(new(gx - 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                            sbw.ExclList.Add(new(new(gx - 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                            sbw.ExclList.Add(new(new(gx - 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                        }
                        if (random.Next(4) == 2)
                        {
                            sbw.ExclList.Add(new(new(gx + 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                            sbw.ExclList.Add(new(new(gx + 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                            sbw.ExclList.Add(new(new(gx + 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                        }
                    }
                structs.Add(blockStrcut);
            };
            GeneratorTerrain = (Noise,chunk, highMap, random, x, y, z, gx, gy) =>
            {
                int num = highMap[x, z] - gy;//和当前的插值
                if (gy > 0 && highMap[x, z] > 0)//地下
                {
                    if (num > 0) chunk[x, y, z] = Materials.Valueof("water").Blockdata();
                    if (num == 0) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                    if (num == -1) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                    if (num == -2) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                    if (num == -3)
                    {
                        if (random.Next(2) == 1) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                        else chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                    }
                }
                else//地上
                {
                    if (num == 1)
                    {
                        if (random.Next(2) == 1) chunk[x, y, z] = Materials.Valueof("bush").Blockdata();
                    }
                    if (num == 0) chunk[x, y, z] = Materials.Valueof("grass").Blockdata();
                    if (num == -1) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num == -2) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num == -3) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                }
                if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
            };
        }
    }
}