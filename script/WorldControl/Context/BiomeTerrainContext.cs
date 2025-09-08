using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace horizoncraft.script.WorldControl
{
    public class BiomeTerrainContext
    {
        public Chunk Chunk;
        public int[,] HighMap;
        public Random Random;
        public float Noise;
        public int LocalX;
        public int LocalY;
        public int GlobalX;
        public int GlobalY;
        public int GlobalZ;
        public BlockData BlockData;


        public void SetBlock(string name, int id = 0)
        {
            Chunk.SetBlock(LocalX, LocalY, GlobalZ, Materials.Valueof(name), id);
        }
    }
}