using Godot;
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
            color = Color.Color8(73, 140, 255);
        }
    }
}