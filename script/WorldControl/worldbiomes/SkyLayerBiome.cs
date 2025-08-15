using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static horizoncraft.script.WorldControl.BiomeManage;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class SkyLayerBiome : Biome
    {

        public SkyLayerBiome()
        {
            name = "天空";
            biomeType = BiomeType.Sky;
            weight = 100;
        }

    }
}