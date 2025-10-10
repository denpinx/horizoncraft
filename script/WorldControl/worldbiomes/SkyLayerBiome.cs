using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.Struct;
using horizoncraft.script.WorldControl.Struct.structs;
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
            DebugColor = Color.Color8(73, 140, 255);
        }
    }
}