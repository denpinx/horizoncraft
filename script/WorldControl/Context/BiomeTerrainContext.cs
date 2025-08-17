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
        public Blockdata Blockdata;
    }
}