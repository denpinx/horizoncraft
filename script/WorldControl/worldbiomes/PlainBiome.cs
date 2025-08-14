using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class PlainBiome : LandBiome
    {
        public PlainBiome()
        {
            name = "平原";
            weight = 1;
            GetHigh = (noise, x, z) => -Math.Abs((int)(noise.GetNoise2D(x * Chunk.Size, z) * 8) - new Random(HashCode.Combine(x, z)).Next(4));
            GeneratorTerrain = new ForestBiome().GeneratorTerrain;
        }
    }
}