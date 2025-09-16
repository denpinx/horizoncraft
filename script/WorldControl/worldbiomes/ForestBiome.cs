using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.Struct;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class ForestBiome : LandBiome
    {
        public ForestBiome()
        {
            name = "森林";
            weight = 3;
            DebugColor = Color.Color8(128, 168, 23);
        }

        public override int GetHigh(Random random, FastNoiseLite noise, int x, int z)
        {
            return random.Next(-32, 10) + random.Next(-32, 10);
        }

        public override void GeneratorStruct(LandBiomeStructContext lbsc)
        {
            if (lbsc.Random.Next(12) != 1) return;
            if (lbsc.GlobalY < 0)
            {
                BlockStruct blockStrcut =
                    StructManage.GetStruct("oak_tree", lbsc.GlobalX, lbsc.GlobalY, lbsc.GloablZ, lbsc.Random);
                lbsc.BlockStructs.Add(blockStrcut);
            }
        }

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY;
            if (context.GlobalY > 0 && context.HighMap[context.LocalX, context.GlobalZ] > 0) //地下
            {
                switch (num)
                {
                    case > 0:
                        context.SetBlock("water");
                        break;
                    case 0:
                        context.SetBlock("sand");
                        break;
                    case -1:
                        context.SetBlock("sand");
                        break;
                    case -2:
                        context.SetBlock("sand");
                        break;
                    case -3:
                        if (context.Random.Next(2) == 1)
                            context.SetBlock("sand");
                        else
                            context.SetBlock("stone");
                        break;
                    case <= -4:
                        context.SetBlock("stone");
                        break;
                }
            }
            else //地上
            {
                switch (num)
                {
                    case 1:
                        if (context.Random.Next(2) == 1)
                            context.SetBlock("bush");
                        break;
                    case 0:
                        context.SetBlock("grass");
                        break;
                    case -1:
                        context.SetBlock("dirt");
                        break;
                    case -2:
                        context.SetBlock("dirt");
                        break;
                    case -3:
                        context.SetBlock("dirt");
                        break;
                    case <= -4:
                        context.SetBlock("stone");
                        break;
                }
            }
        }
    }
}