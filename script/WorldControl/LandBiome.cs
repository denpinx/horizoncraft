using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Horizoncraft.script.WorldControl.Context;
using Horizoncraft.script.WorldControl.Struct;

namespace Horizoncraft.script.WorldControl
{
    public class LandBiome : BaseBiome
    {
        public virtual int GetHigh(Random random, FastNoiseLite noise, int x, int z)
        {
            return 0;
        }

        public virtual void GeneratorTerrain(BiomeTerrainContext context)
        {
        }

        public virtual void GeneratorStruct(LandBiomeStructContext landBiomeStructContext)
        {
        }
    }
}