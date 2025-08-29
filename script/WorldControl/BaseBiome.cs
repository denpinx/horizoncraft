using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace horizoncraft.script.WorldControl
{
    public class BaseBiome
    {
        public string name;
        public int weight;
        public Vector2 weight_range;

        public Color color;

        public T Copy<T>() where T : BaseBiome
        {
            return (T)new BaseBiome()
            {
                name = name,
                weight = weight,
                weight_range = weight_range,
            };
        }
    }
}