using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.WorldControl.Struct;

namespace Horizoncraft.script.WorldControl.Context
{
    public class BiomeStructContext
    {
        public required NeoBlockStructManager NeoBlockStructManager;
        
        public FastNoiseLite FastNoiseLite;
        public Random Random;
        public List<BlockStruct> BlockStructs;
        public int GlobalX;
        public int GlobalY;
        public int Altitude;
    }
}