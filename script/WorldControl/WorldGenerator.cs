using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.Struct;
using static horizoncraft.script.WorldControl.BiomeManage;

namespace horizoncraft.script.WorldControl
{
    public class WorldGenerator
    {
        private static readonly Stopwatch StopWatch = new Stopwatch();
        private static readonly FastNoiseLite FastNoiseLite = new FastNoiseLite();

        public static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            const float tension = 0.5f; // 0.5 为标准 Catmull-Rom
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (-tension * p0 + 3 * tension * p1 - 3 * tension * p2 + tension * p3) * t3 +
                (2 * tension * p0 - 5 * tension * p1 + 4 * tension * p2 - tension * p3) * t2 +
                (-tension * p0 + tension * p2) * t +
                2 * tension * p1
            );
        }

        public static int[,] GetHighMap(int x)
        {
            int[,] highmap = new int[Chunk.Size, Chunk.SizeZ];
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                // 获取相邻区块的关键高度点（确保控制点覆盖当前区块）
                float p0 = GetMixinLandBiome(x - 1).GetHigh(new Random((int)(World.Seed + x - 1)), FastNoiseLite, x - 1, z);
                float p1 = GetMixinLandBiome(x).GetHigh(new Random((int)(World.Seed + x)), FastNoiseLite, x, z);
                float p2 = GetMixinLandBiome(x + 1).GetHigh(new Random((int)(World.Seed + x + 1)), FastNoiseLite, x + 1, z);
                float p3 = GetMixinLandBiome(x + 2).GetHigh(new Random((int)(World.Seed + x + 2)), FastNoiseLite, x + 2, z);
                for (int i = 0; i < Chunk.Size; i++)
                {
                    float t = (float)i / (float)(Chunk.Size - 1);
                    highmap[i, z] = (int)CatmullRom(p0, p1, p2, p3, t);
                }
            }

            return highmap;
        }

        //获取这个区块的结构体
        public static List<BlockStruct> GetStructs(int x, int y, int z)
        {
            LandBiome landbiome = GetMixinLandBiome(x);
            int[,] highmap = GetHighMap(x);
            Random random = new Random(x * 3 + y * 7 + z * 11);
            List<BlockStruct> structs = new();
            BiomeType biomeType = BiomeManage.CheckRange(highmap, x, y);
            //structs.Add(OreManage.GeneratorOre(random, x, y));

            if (biomeType == BiomeType.LandBiome)
            {
                var landBiomeStructContext = new LandBiomeStructContext()
                {
                    FastNoiseLite = FastNoiseLite,
                    BlockStructs = structs,
                    Random = random,
                    GloablZ = z,
                };
                for (int i = 0; i < Chunk.Size; i++)
                {
                    int gx = x * Chunk.Size + i;
                    int gy = highmap[i, z] - 1;
                    int hy = highmap[i, z] - y * Chunk.Size;
                    landBiomeStructContext.GlobalX = gx;
                    landBiomeStructContext.GlobalY = gy;
                    landBiomeStructContext.Altitude = hy;
                    if (hy >= 0 && hy < Chunk.Size)
                    {
                        landbiome.GeneratorStruct(landBiomeStructContext);
                    }
                }

                if (structs.Count > 0)
                    return structs;
            }
            else if (biomeType == BiomeType.Deep)
            {
                var biomeStructContext = new BiomeStructContext()
                {
                    FastNoiseLite = FastNoiseLite,
                    BlockStructs = structs,
                    Random = random,
                    GlobalX = x * Chunk.Size,
                    GlobalY = y * Chunk.Size
                };
                Biome biome = BiomeManage.GetDeepBiome(x, y);
                biome.GeneratorOre(biomeStructContext);
                biome.GeneratorStruct(biomeStructContext);
                if (structs.Count > 0)
                    return structs;
            }
            else if (biomeType == BiomeType.Sky)
            {
                var landBiomeStructContext = new BiomeStructContext()
                {
                    FastNoiseLite = FastNoiseLite,
                    BlockStructs = structs,
                    Random = random,
                    GlobalX = x * Chunk.Size,
                    GlobalY = y * Chunk.Size
                };
                Biome biome = BiomeManage.GetSkyBiome(x, y);
                biome.GeneratorStruct(landBiomeStructContext);
                if (structs.Count > 0)
                    return structs;
            }

            return null;
        }

        public static List<BlockStruct> GetAllOres(int x, int y)
        {
            List<BlockStruct> structs = new();
            for (int i = x - 1; i <= x + 1; i++)
            for (int j = y - 1; j <= y + 1; j++)
            {
                Random random = new Random(i * Int16.MaxValue + j);
                var st = OreManage.GeneratorOre(random, i, j);
                if (st != null) structs.Add(st);
            }

            return structs;
        }

        public static List<BlockStruct> GetAllStructs(int x, int y)
        {
            List<BlockStruct> structs = new();
            for (int k = 0; k <= 1; k++)
            for (int i = x - 1; i <= x + 1; i++)
            for (int j = y - 1; j <= y + 1; j++)
            {
                var st = GetStructs(i, j, k);
                if (st != null) structs.AddRange(st);
            }

            return structs;
        }

        public static (BlockMeta, int) GetStructData(List<BlockStruct> structs, int x, int y, int z)
        {
            for (int i = 0; i < structs.Count; i++)
            {
                (BlockMeta, int) data = structs[i].GetBlockMeta(x, y, z); //里面是字典，理论上是o(1)
                if (data.Item1 != null) return data;
            }

            return (null, 0);
        }

        //生成区块
        //不涉及跨区块生成，跨区块方块，通过使用预知获取周围区块的跨区块结构，每个方块都是独立生成
        //区块只生成一次，之后加载不会再重新生成，花点时间可以理解.

        //单区块平均生成耗时13ms
        public static void Generator(Chunk chunk)
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();
            chunk.spawn = true;
            var landbiome = BiomeManage.GetMixinLandBiome(chunk.X);
            int[,] highmap = GetHighMap(chunk.X);
            List<BlockStruct> structs = GetAllStructs(chunk.X, chunk.Y);
            List<BlockStruct> ores = GetAllOres(chunk.X, chunk.Y);
            //地表生物群系

            BiomeType biomeType = BiomeManage.CheckRange(highmap, chunk.X, chunk.Y);
            if (biomeType == BiomeType.LandBiome)
            {
                chunk.BiomeType = landbiome.name;
                BiomeTerrainContext biomeTerrainContext = new BiomeTerrainContext()
                {
                    Chunk = chunk,
                    HighMap = highmap,
                };
                for (int z = 0; z < Chunk.SizeZ; z++)
                {
                    Random random = new Random(chunk.X * 3 + chunk.Y * 7 + z * 11);
                    biomeTerrainContext.Random = random;
                    for (int x = 0; x < Chunk.Size; x++)
                    for (int y = 0; y < Chunk.Size; y++)
                    {
                        int gx = chunk.X * Chunk.Size + x;
                        int gy = chunk.Y * Chunk.Size + y;
                        biomeTerrainContext.LocalX = x;
                        biomeTerrainContext.LocalY = y;
                        biomeTerrainContext.GlobalZ = z;
                        biomeTerrainContext.GlobalX = gx;
                        biomeTerrainContext.GlobalY = gy;
                        biomeTerrainContext.Noise = FastNoiseLite.GetNoise2D(gx, gy);
                        biomeTerrainContext.BlockData = chunk.GetBlock(x, y, z);
                        landbiome.GeneratorTerrain(biomeTerrainContext);

                        if (gy >= highmap[x, z] && FastNoiseLite.GetNoise2D(gx / 0.5f, gy) > 0.2f && z == 1)
                            chunk.SetBlock(x, y, z, Materials.Valueof("air"));

                        var data = GetStructData(structs, gx, gy, z);
                        if (data.Item1 != null)
                        {
                            // var insideblock = chunk.GetBlock(x, y, z);
                            // if (insideblock.IsMeta("air"))
                            chunk.SetBlock(x, y, z, data.Item1, data.Item2);
                        }
                    }
                }
            }
            else
            {
                Biome biome;
                if (biomeType == BiomeType.Deep)
                {
                    biome = GetDeepBiome(chunk.X, chunk.Y);
                }
                else
                {
                    biome = GetSkyBiome(chunk.X, chunk.Y);
                }

                chunk.BiomeType = biome.name;

                BiomeTerrainContext biomeTerrainContext = new BiomeTerrainContext()
                {
                    Chunk = chunk,
                    HighMap = highmap,
                };
                for (int z = 0; z < Chunk.SizeZ; z++)
                {
                    biomeTerrainContext.Random = new Random(chunk.X * 3 + chunk.Y * 7 + z * 11);
                    for (int x = 0; x < Chunk.Size; x++)
                    for (int y = 0; y < Chunk.Size; y++)
                    {
                        int gx = chunk.X * Chunk.Size + x;
                        int gy = chunk.Y * Chunk.Size + y;
                        biomeTerrainContext.LocalX = x;
                        biomeTerrainContext.LocalY = y;
                        biomeTerrainContext.GlobalZ = z;
                        biomeTerrainContext.GlobalX = gx;
                        biomeTerrainContext.GlobalY = gy;
                        biomeTerrainContext.Noise = FastNoiseLite.GetNoise2D(gx, gy);
                        biomeTerrainContext.BlockData = chunk.GetBlock(x, y, z);
                        
                        
                        if (gy > highmap[x, z] && FastNoiseLite.GetNoise2D(gx / 0.5f, gy) > 0.3f && z == 1)
                            chunk.SetBlock(x, y, z, Materials.Valueof("air"));
                        else if (biomeType == BiomeType.Deep) chunk.SetBlock(x, y, z, Materials.Valueof("stone"));

                        biome.GeneratorTerrain(biomeTerrainContext);

                        var data = GetStructData(structs, gx, gy, z);
                        if (data.Item1 != null)
                        {
                            chunk.SetBlock(x, y, z, data.Item1, data.Item2);
                        }
                    }
                }
            }

            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    int gx = chunk.X * Chunk.Size + x;
                    int gy = chunk.Y * Chunk.Size + y;
                    var ore = GetStructData(ores, gx, gy, 1);
                    if (ore.Item1 != null)
                    {
                        var insideblock = chunk.GetBlock(x, y, 1);
                        if (insideblock.IsMeta("stone"))
                            chunk.SetBlock(x, y, 1, ore.Item1, ore.Item2);
                    }
                }
            }

            chunk.HighMap = highmap;
            chunk.UpdateList.Clear();
            chunk.update_tilemap = true;
            chunk.update_server = true;
            stopWatch.Stop();
            chunk.SpawnCostTime_μs = stopWatch.Elapsed.TotalMicroseconds;
        }
    }
}