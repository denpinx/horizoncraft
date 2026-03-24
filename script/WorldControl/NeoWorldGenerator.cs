using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Horizoncraft.script.WorldControl.Context;
using Horizoncraft.script.WorldControl.Struct;

namespace Horizoncraft.script.WorldControl;

public class NeoWorldGenerator
{
    private readonly Stopwatch StopWatch = new Stopwatch();
    private readonly FastNoiseLite FastNoiseLite = new FastNoiseLite();
    public NeoBiomeManage NeoBiomeManage;
    public NeoOreManage NeoOreManage;
    public NeoBlockStructManager NeoBlockStructManager;
    public NeoWorldGenerator()
    {
        NeoBiomeManage = new NeoBiomeManage();
        NeoOreManage = new  NeoOreManage();
        NeoBlockStructManager = new NeoBlockStructManager();
        
        NeoBlockStructManager.LoadBuilds();
    }
    /// <summary>
    /// 插值计算
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public float CatmullRom(float p0, float p1, float p2, float p3, float t)
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

    /// <summary>
    /// 获取区块的高度图
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public int[,] GetHighMap(int x)
    {
        int[,] highmap = new int[Chunk.Size, Chunk.SizeZ];
        for (int z = 0; z < Chunk.SizeZ; z++)
        {
            // 获取相邻区块的关键高度点（确保控制点覆盖当前区块）
            float p0 = NeoBiomeManage.GetMixinLandBiome(x - 1)
                .GetHigh(new Random((int)(World.Seed + x - 1)), FastNoiseLite, x - 1, z);
            float p1 = NeoBiomeManage.GetMixinLandBiome(x).GetHigh(new Random((int)(World.Seed + x)), FastNoiseLite, x, z);
            float p2 = NeoBiomeManage.GetMixinLandBiome(x + 1)
                .GetHigh(new Random((int)(World.Seed + x + 1)), FastNoiseLite, x + 1, z);
            float p3 = NeoBiomeManage.GetMixinLandBiome(x + 2)
                .GetHigh(new Random((int)(World.Seed + x + 2)), FastNoiseLite, x + 2, z);
            for (int i = 0; i < Chunk.Size; i++)
            {
                float t = (float)i / (float)(Chunk.Size - 1);
                highmap[i, z] = (int)CatmullRom(p0, p1, p2, p3, t);
            }
        }

        return highmap;
    }

    //获取这个区块的结构体
    public List<BlockStruct> GetStructs(int x, int y, int z)
    {
        LandBiome landbiome = NeoBiomeManage.GetMixinLandBiome(x);
        int[,] highmap = GetHighMap(x);
        Random random = new Random(x * 3 + y * 7 + z * 11);
        List<BlockStruct> structs = new();
        BiomeType biomeType = NeoBiomeManage.CheckRange(highmap, x, y);

        if (biomeType == BiomeType.LandBiome)
        {
            var landBiomeStructContext = new LandBiomeStructContext()
            {
                NeoBlockStructManager = NeoBlockStructManager,
                FastNoiseLite = FastNoiseLite,
                BlockStructs = structs,
                HighMap = highmap,
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
                NeoBlockStructManager = NeoBlockStructManager,
                FastNoiseLite = FastNoiseLite,
                GlobalX = x * Chunk.Size,
                GlobalY = y * Chunk.Size,
                BlockStructs = structs,
                Random = random,
            };
            Biome biome = NeoBiomeManage.GetDeepBiome(x, y);
            biome.GeneratorStruct(biomeStructContext);
            if (structs.Count > 0)
                return structs;
        }
        else if (biomeType == BiomeType.Sky)
        {
            var landBiomeStructContext = new BiomeStructContext()
            {
                NeoBlockStructManager = NeoBlockStructManager,
                FastNoiseLite = FastNoiseLite,
                GlobalX = x * Chunk.Size,
                GlobalY = y * Chunk.Size,
                BlockStructs = structs,
                Random = random,
            };
            Biome biome = NeoBiomeManage.GetSkyBiome(x, y);
            biome.GeneratorStruct(landBiomeStructContext);
            if (structs.Count > 0)
                return structs;
        }

        return null;
    }

    public List<BlockStruct> GetAllOres(int x, int y)
    {
        List<BlockStruct> structs = new();
        for (int i = x - 1; i <= x + 1; i++)
        for (int j = y - 1; j <= y + 1; j++)
        {
            Random random = new Random(i * Int16.MaxValue + j);
            var st = NeoOreManage.GeneratorOre(random, i, j);
            if (st != null) structs.Add(st);
        }

        return structs;
    }

    public List<BlockStruct> GetAllStructs(int x, int y)
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

    public (BlockMeta, int) GetStructData(List<BlockStruct> structs, int x, int y, int z)
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
    public void Generator(Chunk chunk)
    {
        Stopwatch stopWatch = new();
        stopWatch.Start();
        chunk.spawn = true;
        var landbiome = NeoBiomeManage.GetMixinLandBiome(chunk.X);
        int[,] highmap = GetHighMap(chunk.X);
        List<BlockStruct> structs = GetAllStructs(chunk.X, chunk.Y);
        List<BlockStruct> ores = GetAllOres(chunk.X, chunk.Y);
        //地表生物群系

        BiomeType biomeType = NeoBiomeManage.CheckRange(highmap, chunk.X, chunk.Y);
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
                biome = NeoBiomeManage.GetDeepBiome(chunk.X, chunk.Y);
            }
            else
            {
                biome = NeoBiomeManage.GetSkyBiome(chunk.X, chunk.Y);
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