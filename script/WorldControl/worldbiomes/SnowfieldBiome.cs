using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class SnowfieldBiome : LandBiome
    {
        public SnowfieldBiome()
        {
            name = "雪地";
            weight = 2;
        }

        public override int GetHigh(FastNoiseLite noise, int x, int z)
        {
            return (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8);
        }

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY;
            switch (num)
            {
                case 0:
                    context.Blockdata.SetMeta("snow");
                    break;
                case -1:
                    context.Blockdata.SetMeta("snow");
                    break;
                case -2:
                    context.Blockdata.SetMeta("snow");
                    break;
                case -3:
                    context.Blockdata.SetMeta("snow");
                    break;
                case <= -4:
                    context.Blockdata.SetMeta("stone");
                    break;
            }
        }

        public override void GeneratorStruct(LandBiomeStructContext lbsc)
        {
            if (lbsc.Random.Next(14) != 1) return;
            BlockStruct blockStrcut = new BlockStruct();
            if (lbsc.GlobalY < 0)
                for (int h = 0; h < 7 + lbsc.Random.Next(6); h++)
                {
                    blockStrcut.AddBlock(lbsc.GlobalX, lbsc.GlobalY - h, lbsc.GloablZ, Materials.Valueof("oak_log"), 0);
                    //随机分支
                    if (lbsc.Random.Next(4) == 1)
                    {
                        blockStrcut.AddBlock(lbsc.GlobalX - 1, lbsc.GlobalY - h, lbsc.GloablZ,
                            Materials.Valueof("oak_log"), 1);
                        blockStrcut.AddBlock(lbsc.GlobalX - 1, lbsc.GlobalY - h - 1, lbsc.GloablZ,
                            Materials.Valueof("oak_leaves"), 0);
                        blockStrcut.AddBlock(lbsc.GlobalX - 2, lbsc.GlobalY - h, lbsc.GloablZ,
                            Materials.Valueof("oak_leaves"), 0);
                    }

                    if (lbsc.Random.Next(4) == 2)
                    {
                        blockStrcut.AddBlock(lbsc.GlobalX + 1, lbsc.GlobalY - h, lbsc.GloablZ,
                            Materials.Valueof("oak_log"), 1);
                        blockStrcut.AddBlock(lbsc.GlobalX + 1, lbsc.GlobalY - h - 1, lbsc.GloablZ,
                            Materials.Valueof("oak_leaves"), 0);
                        blockStrcut.AddBlock(lbsc.GlobalX + 2, lbsc.GlobalY - h, lbsc.GloablZ,
                            Materials.Valueof("oak_leaves"), 0);
                    }
                }

            lbsc.BlockStructs.Add(blockStrcut);
        }
    }
}