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
            color = Color.Color8(80, 255, 80);
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
                        context.SetBlock("water");
                        break;
                    case 0:
                        context.SetBlock("sand");
                        break;
                    case -1:
                        context.SetBlock("sand");
                        break;
                    case -2:
                        context.SetBlock("sand");
                        break;
                    case -3:
                        if (context.Random.Next(2) == 1)
                            context.SetBlock("sand");
                        else
                            context.SetBlock("stone");
                        break;
                    case <= -4:
                        context.SetBlock("stone");
                        break;
                }
            }
            else //地上
            {
                switch (num)
                {
                    case 1:
                        if (context.Random.Next(2) == 1)
                            context.SetBlock("bush");
                        break;
                    case 0:
                        context.SetBlock("grass");
                        break;
                    case -1:
                        context.SetBlock("dirt");
                        break;
                    case -2:
                        context.SetBlock("dirt");
                        break;
                    case -3:
                        context.SetBlock("dirt");
                        break;
                    case <= -4:
                        context.SetBlock("stone");
                        break;
                }
            }
        }
    }
}