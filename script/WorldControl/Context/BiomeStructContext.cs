using System;
using System.Collections.Generic;
using Godot;

namespace Horizoncraft.script.WorldControl.Context
{
    public class BiomeStructContext
    {
        public FastNoiseLite FastNoiseLite;
        public Random Random;
        public List<BlockStruct> BlockStructs;
        public int GlobalX;
        public int GlobalY;
        public int Altitude;
    }
}