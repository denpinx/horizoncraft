using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.Context;
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
                float p0 = GetLandBiome(x - 1).GetHigh(FastNoiseLite, x - 1, z);
                float p1 = GetLandBiome(x).GetHigh(FastNoiseLite, x, z);
                float p2 = GetLandBiome(x + 1).GetHigh(FastNoiseLite, x + 1, z);
                float p3 = GetLandBiome(x + 2).GetHigh(FastNoiseLite, x + 2, z);
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
            LandBiome landbiome = GetLandBiome(x);
            int[,] highmap = GetHighMap(x);
            Random random = new Random(x * 3 + y * 7 + z * 11);
            List<BlockStruct> structs = new();
            BiomeType biomeType = BiomeManage.CheckRange(highmap, x, y);
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
                    if (hy >= 0 && hy < Chunk.Size)
                    {
                        landBiomeStructContext.GlobalX = gx;
                        landBiomeStructContext.GlobalY = gy;
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
                (BlockMeta, int) data = structs[i].GetBlockMeta(x, y, z);
                if (data.Item1 != null) return data;
            }

            return (null, 0);
        }

        //生成区块
        //不涉及跨区块生成，跨区块方块，通过使用预知获取周围区块的跨区块结构，每个方块都是独立生成
        //区块只生成一次，之后加载不会再重新生成，花点时间可以理解.
        //旧算法每次设置方块都需要遍历所有的区块才能命中，以及还要等待区块异步加载的延迟，现在直接100%命中
        //当前运行速度50chunk+/s
        //去除stopwatch的误差，单区块平均生成耗时小于1ms

        /*
            单线程生成 1000 区块耗时12669 ms
            平均耗时12 ms
            多线程生成 1000 区块耗时2528 ms
            平均耗时2 ms
         */
        public static void Generator(Chunk chunk)
        {
            StopWatch.Restart();
            chunk.spawn = true;
            var landbiome = BiomeManage.GetLandBiome(chunk.X);
            int[,] highmap = GetHighMap(chunk.X);
            List<BlockStruct> structs = GetAllStructs(chunk.X, chunk.Y);
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
                        biomeTerrainContext.Blockdata = chunk.GetBlock(x, y, z);
                        landbiome.GeneratorTerrain(biomeTerrainContext);

                        if (gy >= highmap[x, z] && FastNoiseLite.GetNoise2D(gx / 0.5f, gy) > 0.2f && z == 1)
                            chunk.SetBlock(x, y, z, Materials.Valueof("air"));

                        var data = GetStructData(structs, gx, gy, z);
                        if (data.Item1 != null)
                            chunk.SetBlock(x, y, z, data.Item1, data.Item2);
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
                        biomeTerrainContext.Blockdata = chunk.GetBlock(x, y, z);
                        if (gy > highmap[x, z] && FastNoiseLite.GetNoise2D(gx / 0.5f, gy) > 0.3f && z == 1)
                            chunk.SetBlock(x, y, z, Materials.Valueof("air"));
                        else if (biomeType == BiomeType.Deep) chunk.SetBlock(x, y, z, Materials.Valueof("stone"));

                        biome.GeneratorTerrain(biomeTerrainContext);

                        var data = GetStructData(structs, gx, gy, z);
                        if (data.Item1 != null)
                            chunk.SetBlock(x, y, z, data.Item1, data.Item2);
                    }
                }
            }

            StopWatch.Stop();

            chunk.HighMap = highmap;
            chunk.UpdateList.Clear();
            chunk.SpawnCostTime = (int)StopWatch.ElapsedMilliseconds;
            chunk.update_tilemap = true;
            chunk.update_server = true;
        }
    }
}