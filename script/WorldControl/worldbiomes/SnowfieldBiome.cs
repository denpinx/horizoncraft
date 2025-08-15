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
            return  (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8);
        }

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GloablZ] - context.GlobalY;
            switch (num)
            {
                case 0:
                    context.Chunk[context.LocalX, context.LocalY, context.GloablZ] = Materials.Valueof("snow").Blockdata();
                    break;
                case -1:
                    context.Chunk[context.LocalX, context.LocalY, context.GloablZ] = Materials.Valueof("snow").Blockdata();
                    break;
                case -2:
                    context.Chunk[context.LocalX, context.LocalY, context.GloablZ] = Materials.Valueof("snow").Blockdata();
                    break;
                case -3:
                    context.Chunk[context.LocalX, context.LocalY, context.GloablZ] = Materials.Valueof("snow").Blockdata();
                    break;
                case < -4:
                    context.Chunk[context.LocalX, context.LocalY, context.GloablZ] = Materials.Valueof("stone").Blockdata();
                    break;
            }
        }

        public override void GeneratorStruct(LandBiomeStructContext landBiomeStructContext)
        {
        }
    }
}