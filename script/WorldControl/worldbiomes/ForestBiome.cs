using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class ForestBiome : LandBiome
    {
        public ForestBiome()
        {
            name = "森林";
            weight = 3;
        }

        public override int GetHigh(FastNoiseLite noise, int x, int z)
        {
            return ((int)(noise.GetNoise2D(x * Chunk.Size, z) * 64)) - new Random(HashCode.Combine(x, z)).Next(8);
        }

        public override void GeneratorStruct(LandBiomeStructContext lbsc)
        {
            if (lbsc.Random.Next(7) != 1) return;
            BlockStruct blockStrcut = new BlockStruct();
            if (lbsc.GlobalY < 0)
                for (int h = 0; h < 5 + lbsc.Random.Next(4); h++)
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

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY;
            if (context.GlobalY > 0 && context.HighMap[context.LocalX, context.GlobalZ] > 0) //地下
            {
                switch (num)
                {
                    case > 0:
                        context.Blockdata.SetMeta("water");
                        break;
                    case 0:
                        context.Blockdata.SetMeta("sand");
                        break;
                    case -1:
                        context.Blockdata.SetMeta("sand");
                        break;
                    case -2:
                        context.Blockdata.SetMeta("sand");
                        break;
                    case -3:
                        if (context.Random.Next(2) == 1)
                            context.Blockdata.SetMeta("sand");
                        else
                            context.Blockdata.SetMeta("stone");
                        break;
                    case <= -4:
                        context.Blockdata.SetMeta("stone");
                        break;
                }
            }
            else //地上
            {
                switch (num)
                {
                    case 1:
                        if (context.Random.Next(2) == 1)
                            context.Blockdata.SetMeta("bush");
                        break;
                    case 0:
                        context.Blockdata.SetMeta("grass");
                        break;
                    case -1:
                        context.Blockdata.SetMeta("dirt");
                        break;
                    case -2:
                        context.Blockdata.SetMeta("dirt");
                        break;
                    case -3:
                        context.Blockdata.SetMeta("dirt");
                        break;
                    case <= -4:
                        context.Blockdata.SetMeta("stone");
                        break;
                }
            }
        }
    }
}