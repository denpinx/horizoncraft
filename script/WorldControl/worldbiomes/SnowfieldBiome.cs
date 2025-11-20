using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Horizoncraft.script.WorldControl.Context;
using Horizoncraft.script.WorldControl.Struct;

namespace Horizoncraft.script.WorldControl.worldbiomes
{
    public class SnowfieldBiome : LandBiome
    {
        public SnowfieldBiome()
        {
            name = "雪地";
            weight = 2;
            DebugColor = Color.Color8(255, 255, 255);
        }

        public override int GetHigh(Random random, FastNoiseLite noise, int x, int z)
        {
            return random.Next(-9, -4);
        }

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY;
            switch (num)
            {
                case 0:
                    context.SetBlock("snow");
                    break;
                case -1:
                    context.SetBlock("snow");
                    break;
                case -2:
                    context.SetBlock("snow");
                    break;
                case -3:
                    context.SetBlock("snow");
                    break;
                case <= -4:
                    context.SetBlock("stone");
                    break;
            }
        }

        public override void GeneratorStruct(LandBiomeStructContext lbsc)
        {
            if (lbsc.GlobalY < 0)
            {
                if (lbsc.Random.Next(16) == 1)
                {
                    BlockStruct blockStrcut =
                        BlockStructManager.GetStruct("spruce_tree", lbsc.GlobalX, lbsc.GlobalY, lbsc.GloablZ,
                            lbsc.Random);
                    lbsc.BlockStructs.Add(blockStrcut);
                }
            }
        }
    }
}