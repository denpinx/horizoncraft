using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace horizoncraft.script.WorldControl
{
    public class LandBiome : BaseBiome
    {
        public Func<FastNoiseLite, int, int, int> GetHigh;
        public Action<Chunk, int[,], Random, int, int, int, int, int> GeneratorTerrain;
        //噪音，随机生成器，globalX,globalY,z
        public Action<FastNoiseLite, Random, List<BlockStruct>, int, int, int> GeneratorStrcut;
    }
}