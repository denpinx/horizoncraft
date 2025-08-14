using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static horizoncraft.script.WorldControl.BiomeManage;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class DeepLayerBiome : Biome
    {
        public DeepLayerBiome()
        {
            name = "深层";
            biomeType = BiomeType.Deep;
            weight = 100;
            GeneratorTerrain = (Chunk, highmap, noise, x, y, z, gx, gy) =>
            {
                Chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
            };
        }
    }
}