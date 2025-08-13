using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace horizoncraft.script.WorldControl.Work
{
    public class Biome
    {
        public string name;
        public int weight;
        public int Left_weight;
        public int Right_weight;
        public Func<FastNoiseLite,int,int,int> GetHigh;
        public Action<Chunk, int[,], List<BlockStrcut>> GeneratorTerrain;
        public Func<FastNoiseLite, int, int, int, List<BlockStrcut>> GeneratorStrcut;
    }
}