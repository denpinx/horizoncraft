using Godot;

namespace Horizoncraft.script.WorldControl
{
    public class BaseBiome
    {
        public string name;
        public int weight;
        public Vector2 weight_range;

        public Color DebugColor;

        public BiomeItem GetBiomeItem()
        {
            return new BiomeItem()
            {
                Name = name,
                Weight = weight,
                WeightRange = weight_range,
            };
        }
    }
}