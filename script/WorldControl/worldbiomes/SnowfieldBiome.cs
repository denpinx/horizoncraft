using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class SnowfieldBiome : LandBiome
    {
        public SnowfieldBiome()
        {
            name = "雪地";
            weight = 2;
            GetHigh = (noise, x, z) => (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8);
            GeneratorTerrain = (Noise,chunk, highMap, random, x, y, z, gx, gy) =>
            {
                int num = highMap[x, z] - gy;
                if (num == 0) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                if (num == -1) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                if (num == -2) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                if (num == -3) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
            };
            GeneratorStruct = (noise, random, structs, gx, gy, z) =>
            {
                if (random.Next(14) != 1) return;
                BlockStruct blockStrcut = new BlockStruct();
                SetBlockWork sbw = blockStrcut.work;
                if (gy < 0)
                    for (int h = 0; h < 8 + random.Next(6); h++)
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
        }
    }
}