using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;
using Horizoncraft.script.WorldControl.Context;
using HorizonCraft.script.WorldControl.Context;
using Horizoncraft.script.WorldControl.Struct;
using static Horizoncraft.script.WorldControl.BiomeManage;
using Vector2 = Godot.Vector2;

namespace Horizoncraft.script.WorldControl
{
    public class Biome : BaseBiome
    {
        public BiomeType biomeType = BiomeType.Deep;

        //生成二维地形
        //public Action<Chunk, int[,], float, int, int, int, int, int> GeneratorTerrain;
        //噪音，随机生成器，globalX,globalY,z
        //public Action<FastNoiseLite, Random, List<BlockStruct>, int, int> GeneratorStruct;
        public virtual void GeneratorTerrain(BiomeTerrainContext biomeTerrainContext)
        {
        }

        public virtual void GeneratorStruct(BiomeStructContext landBiomeStructContext)
        {
        }
    }
}