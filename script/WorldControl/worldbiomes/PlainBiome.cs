using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.Context;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class PlainBiome : LandBiome
    {
        public PlainBiome()
        {
            name = "平原";
            weight = 1;
        }

        public override int GetHigh(FastNoiseLite noise, int x, int z)
        {
            return -Math.Abs(
                (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8) - new Random(HashCode.Combine(x, z)).Next(4));
        }

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY; //和当前的插值
            if (context.GlobalY > 0 && context.HighMap[context.LocalX, context.GlobalZ] > 0) //地下
            {
                switch (num)
                {
                    case > 0:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("water").Blockdata();
                        break;
                    case 0:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        break;
                    case -1:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        break;
                    case -2:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        break;
                    case -3:
                        if (context.Random.Next(2) == 1)
                            context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        else context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("stone").Blockdata();
                        break;
                    case <= -4:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("stone").Blockdata();
                        break;
                }
            }
            else //地上
            {
                switch (num)
                {
                    case 1:
                        if (context.Random.Next(2) == 1)
                            context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("bush").Blockdata();
                        break;
                    case 0:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("grass").Blockdata();
                        break;
                    case -1:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("dirt").Blockdata();
                        break;
                    case -2:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("dirt").Blockdata();
                        break;
                    case -3:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("dirt").Blockdata();
                        break;
                    case <= -4:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("stone").Blockdata();
                        break;
                }
            }
        }
    }
}