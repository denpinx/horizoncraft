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
        public Action<Chunk, int[,],List<BlockStrcut>,Random,int,int,int,int,int> GeneratorTerrain;
        //噪音，随机生成器，globalX,globalY,z
        public Action<FastNoiseLite, Random,List<BlockStrcut>, int, int, int> GeneratorStrcut;
    }
}