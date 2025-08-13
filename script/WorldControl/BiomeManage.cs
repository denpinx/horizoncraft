using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.work;
using horizoncraft.script.WorldControl.Work;
using MemoryPack.Compression;

namespace horizoncraft.script.WorldControl
{
    public class BiomeManage
    {
        public static FastNoiseLite biome_noise = new FastNoiseLite();
        public static List<Biome> biomes = new List<Biome>();
        public static int MaxWeight = 0;
        public static void Register(Biome biome)
        {
            biome.weight = (int)(biome.weight + biomes.Count * 1.25 * biome.weight);
            biome.Left_weight = MaxWeight;
            biome.Right_weight = MaxWeight + biome.weight;
            MaxWeight += biome.weight;
            biomes.Add(biome);
        }


        // 平滑计算当前X轴的生物群系类型
        // 但是这里的biome_noise.GetNoise1D(x) ，这个值越接近1，越难生成，导致分布并不是按照MaxWeight来分布的
        public static Biome Valueof(string name)
        {
            for (int i = 0; i < biomes.Count; i++)
                if (biomes[i].name == name) return biomes[i];
            return null;
        }
        public static Biome GetBiome(int x)
        {
            var num = (int)(biome_noise.GetNoise1D(x) * MaxWeight);
            num = Mathf.Abs(num);
            for (int i = 0; i < biomes.Count; i++)
            {
                if (num >= biomes[i].Left_weight && num < biomes[i].Right_weight)
                {
                    return biomes[i];
                }
            }
            return null;
        }
        static BiomeManage()
        {
            //森林
            Register(new Biome
            {
                name = "森林",
                weight = 3,
                GetHigh = (noise, x, z) => ((int)(noise.GetNoise2D(x * Chunk.Size, z) * 64)) - new Random(HashCode.Combine(x, z)).Next(8),
                GeneratorTerrain = (chunk, highMap, blockStrcut) =>
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Random random = new Random(chunk.X * 3 + chunk.Y * 7 + z * 11);
                        for (int x = 0; x < Chunk.Size; x++)
                            for (int y = 0; y < Chunk.Size; y++)
                            {
                                int gx = chunk.X * Chunk.Size + x;
                                int gy = chunk.Y * Chunk.Size + y;
                                int num = highMap[x, z] - gy;//和当前的插值
                                if (gy > 0 && highMap[x, z] > 0)//地下
                                {
                                    if (num > 0) chunk[x, y, z] = Materials.Valueof("water").Blockdata();
                                    if (num == 0) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                                    if (num == -1) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                                    if (num == -2) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                                    if (num == -3)
                                    {
                                        if (random.Next(2) == 1) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                                        else chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                                    }
                                }
                                else//地上
                                {
                                    if (num == 1)
                                    {
                                        if (random.Next(2) == 1) chunk[x, y, z] = Materials.Valueof("bush").Blockdata();
                                    }
                                    if (num == 0) chunk[x, y, z] = Materials.Valueof("grass").Blockdata();
                                    if (num == -1) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                                    if (num == -2) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                                    if (num == -3) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                                    if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                                }

                                if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();

                                (BlockMeta, int) data = WorldGenerator.GetStructData(blockStrcut, gx, gy, z);
                                if (data.Item1 != null)
                                {
                                    chunk[x, y, z] = data.Item1.Blockdata();
                                    chunk[x, y, z].STATE = data.Item2;
                                }
                            }
                    }
                },
                GeneratorStrcut = (noise, x, y, z) =>
                {
                    List<BlockStrcut> strcuts = new List<BlockStrcut>();
                    int[,] highmap = WorldGenerator.GetHighMap(x);
                    Random random = new Random(x * 3 + y * 7 + z * 11);
                    for (int i = 0; i < Chunk.Size; i++)
                    {
                        int gx = x * Chunk.Size + i;
                        int hy = highmap[i, z] - y * Chunk.Size;
                        if (hy >= 0 && hy < Chunk.Size && random.Next(7) == 1)
                        {
                            int gy = highmap[i, z] - 1;
                            BlockStrcut blockStrcut = new BlockStrcut();
                            SetBlockWork sbw = blockStrcut.work;
                            if (gy < 0)
                                for (int h = 0; h < 5 + random.Next(4); h++)
                                {
                                    sbw.ExclList.Add(new(new(gx, gy - h, z), Materials.Valueof("oak_log"), 0));
                                    //随机分支
                                    if (random.Next(4) == 1)
                                    {
                                        sbw.ExclList.Add(new(new(gx - 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                                        sbw.ExclList.Add(new(new(gx - 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                                        sbw.ExclList.Add(new(new(gx - 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                                    }
                                    if (random.Next(4) == 2)
                                    {
                                        sbw.ExclList.Add(new(new(gx + 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                                        sbw.ExclList.Add(new(new(gx + 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                                        sbw.ExclList.Add(new(new(gx + 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                                    }
                                }
                            strcuts.Add(blockStrcut);
                        }
                    }
                    return strcuts;
                }
            });
            //平原
            Register(new Biome
            {
                name = "平原",
                weight = 1,
                GetHigh = (noise, x, z) => -Math.Abs((int)(noise.GetNoise2D(x * Chunk.Size, z) * 8) - new Random(HashCode.Combine(x, z)).Next(4)),
                GeneratorTerrain = Valueof("森林").GeneratorTerrain
            });
            //雪地
            Register(new Biome
            {
                name = "雪地",
                weight = 2,
                GetHigh = (noise, x, z) => (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8),
                GeneratorTerrain = (chunk, highMap, blockstructs) =>
                {
                    for (int z = 0; z < 2; z++)
                    {
                        for (int x = 0; x < Chunk.Size; x++)
                            for (int y = 0; y < Chunk.Size; y++)
                            {
                                int gx = chunk.X * Chunk.Size + x;
                                int gy = chunk.Y * Chunk.Size + y;
                                int num = highMap[x, z] - gy;
                                if (num >= 1)
                                {
                                    (BlockMeta, int) data = WorldGenerator.GetStructData(blockstructs, gx, gy, z);
                                    if (data.Item1 != null)
                                    {
                                        chunk[x, y, z] = data.Item1.Blockdata();
                                        chunk[x, y, z].STATE = data.Item2;
                                    }
                                }
                                if (num == 0) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                                if (num == -1) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                                if (num == -2) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                                if (num == -3) chunk[x, y, z] = Materials.Valueof("snow").Blockdata();
                                if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                            }
                    }
                },
                GeneratorStrcut = (noise, x, y, z) =>
                {
                    List<BlockStrcut> strcuts = new List<BlockStrcut>();
                    int[,] highmap = WorldGenerator.GetHighMap(x);
                    Random random = new Random(x * 3 + y * 7 + z * 11);
                    for (int i = 0; i < Chunk.Size; i++)
                    {
                        int gx = x * Chunk.Size + i;
                        int hy = highmap[i, z] - y * Chunk.Size;
                        if (hy >= 0 && hy < Chunk.Size && random.Next(15) == 1)
                        {
                            int gy = highmap[i, z] - 1;
                            BlockStrcut blockStrcut = new BlockStrcut();
                            SetBlockWork sbw = blockStrcut.work;
                            if (gy < 0)
                                for (int h = 0; h < 8 + random.Next(6); h++)
                                {
                                    sbw.ExclList.Add(new(new(gx, gy - h, z), Materials.Valueof("oak_log"), 0));
                                    //随机分支
                                    if (random.Next(4) == 1)
                                    {
                                        sbw.ExclList.Add(new(new(gx - 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                                        sbw.ExclList.Add(new(new(gx - 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                                        sbw.ExclList.Add(new(new(gx - 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                                    }
                                    if (random.Next(4) == 2)
                                    {
                                        sbw.ExclList.Add(new(new(gx + 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                                        sbw.ExclList.Add(new(new(gx + 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                                        sbw.ExclList.Add(new(new(gx + 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                                    }
                                }
                            strcuts.Add(blockStrcut);
                        }
                    }
                    return strcuts;
                }
            });
        }
    }
}