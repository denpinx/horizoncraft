using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl.work;
using horizoncraft.script.WorldControl.worldbiomes;
using MemoryPack.Compression;

namespace horizoncraft.script.WorldControl
{
    public class BiomeManage
    {
        public enum BiomeType
        {
            LandBiome,
            Sky,
            Deep
        }

        public static FastNoiseLite biome_noise = new FastNoiseLite();

        public static List<LandBiome> prim_landbiomes = new List<LandBiome>();
        public static List<LandBiome> Mixin_Landbiomes = new List<LandBiome>();
        public static List<Biome> deep_biomes = new List<Biome>();
        public static List<Biome> sky_biomes = new List<Biome>();
        public static float LandMaxWeight = 0;
        public static float DeepMaxWeight = 0;
        public static float SkyMaxWeight = 0;

        public static void Register(BaseBiome basebiome)
        {
            if (basebiome is LandBiome landBiome)
            {
                prim_landbiomes.Add(landBiome);
                LandMaxWeight += basebiome.weight;
            }
            else if (basebiome is Biome biome)
            {
                if (biome.biomeType == BiomeType.Deep)
                {
                    DeepMaxWeight += basebiome.weight;
                    deep_biomes.Add(biome);
                }
                else
                {
                    SkyMaxWeight += basebiome.weight;
                    sky_biomes.Add(biome);
                }
            }
        }


        // 平滑计算当前X轴的生物群系类型
        public static LandBiome Valueof(string name)
        {
            for (int i = 0; i < prim_landbiomes.Count; i++)
                if (prim_landbiomes[i].name == name)
                    return prim_landbiomes[i];
            return null;
        }


        //只调用一次
        //将群戏放大化，使得权重分配更均匀
        private static void Amplification()
        {
            //步骤一 搅拌和切砍
            // 2-4-8-16-32
            for (int i = 0; i < 5; i++)
                Mixin(8);

            //todo 步骤二, 添加过度群戏

            //步骤三，分配权重
            int MaxWeight = 0;
            for (int i = 0; i < Mixin_Landbiomes.Count; i++)
                MaxWeight += Mixin_Landbiomes[i].weight;
            float half = -MaxWeight / 2;
            for (int i = 0; i < Mixin_Landbiomes.Count; i++)
            {
                Mixin_Landbiomes[i].weight_range.X = half;
                Mixin_Landbiomes[i].weight_range.Y = half + Mixin_Landbiomes[i].weight;
                half += Mixin_Landbiomes[i].weight;
            }
        }

        private static void Mixin(int time)
        {
            if (Mixin_Landbiomes.Count == 0)
            {
                //复制两遍
                for (int i = 0; i < prim_landbiomes.Count; i++)
                {
                    Mixin_Landbiomes.Add(prim_landbiomes[i].Copy<LandBiome>());
                    Mixin_Landbiomes.Add(prim_landbiomes[i].Copy<LandBiome>());
                }
            }
            else
            {
                //自我复制一遍
                for (int i = 0; i < Mixin_Landbiomes.Count; i++)
                    Mixin_Landbiomes.Add(Mixin_Landbiomes[i].Copy<LandBiome>());
            }

            Random rnd = new Random();
            for (int i = 0; i < time; i++)
            {
                int v1 = rnd.Next(Mixin_Landbiomes.Count);
                int v2 = rnd.Next(Mixin_Landbiomes.Count);
                if (v1 != v2)
                {
                    var temp = Mixin_Landbiomes[v1];
                    Mixin_Landbiomes[v1] = Mixin_Landbiomes[v2];
                    Mixin_Landbiomes[v2] = temp;
                }
            }
        }

        public static void ReSetWeight()
        {
            float half = -LandMaxWeight / 2;
            for (int i = 0; i < prim_landbiomes.Count; i++)
            {
                prim_landbiomes[i].weight_range.X = half;
                prim_landbiomes[i].weight_range.Y = half + prim_landbiomes[i].weight;
                half += prim_landbiomes[i].weight;
                GD.Print(
                    $"群系名称：{prim_landbiomes[i].name} 权重:{prim_landbiomes[i].weight}，{prim_landbiomes[i].weight_range.X},{prim_landbiomes[i].weight_range.Y}");
            }

            half = -DeepMaxWeight / 2;
            for (int i = 0; i < deep_biomes.Count; i++)
            {
                deep_biomes[i].weight_range.X = half;
                deep_biomes[i].weight_range.Y = half + deep_biomes[i].weight;
                half += deep_biomes[i].weight;
                GD.Print(
                    $"群系名称：{deep_biomes[i].name} 权重:{deep_biomes[i].weight}，{deep_biomes[i].weight_range.X},{deep_biomes[i].weight_range.Y}");
            }

            half = -SkyMaxWeight / 2;
            for (int i = 0; i < sky_biomes.Count; i++)
            {
                sky_biomes[i].weight_range.X = half;
                sky_biomes[i].weight_range.Y = half + sky_biomes[i].weight;
                half += sky_biomes[i].weight;
                GD.Print(
                    $"群系名称：{sky_biomes[i].name} 权重:{sky_biomes[i].weight}，{sky_biomes[i].weight_range.X},{sky_biomes[i].weight_range.Y}");
            }
        }


        //检查生物群系属于地表群系还是二维群系
        public static BiomeType CheckRange(int[,] HighMap, int x, int y)
        {
            int gx = Chunk.Size * x;
            int gy = Chunk.Size * y;
            int IsDeep = 0;
            int IsSky = 0;
            for (int z = 0; z < Chunk.SizeZ; z++)
            for (int i = 0; i < Chunk.Size; i++)
            {
                if (HighMap[i, z] >= gy - Chunk.Size && HighMap[i, z] < gy + Chunk.Size)
                {
                    return BiomeType.LandBiome;
                }

                if (HighMap[i, z] > gy + Chunk.Size)
                {
                    IsSky++;
                }

                if (HighMap[i, z] < gy - Chunk.Size)
                {
                    IsDeep++;
                }
            }

            if (IsDeep == Chunk.Size * Chunk.SizeZ) return BiomeType.Deep;
            if (IsSky == Chunk.Size * Chunk.SizeZ) return BiomeType.Sky;
            return BiomeType.LandBiome;
        }

        //地表群系
        public static LandBiome GetLandBiome(int x)
        {
            var num = biome_noise.GetNoise1D((float)x) * ((float)LandMaxWeight / 2f);
            for (int i = 0; i < prim_landbiomes.Count; i++)
            {
                if (num >= prim_landbiomes[i].weight_range.X && num < prim_landbiomes[i].weight_range.Y)
                {
                    return prim_landbiomes[i];
                }
            }

            return null;
        }

        public static Biome GetDeepBiome(int x, int y)
        {
            var num = biome_noise.GetNoise2D(x, y) * (DeepMaxWeight / 2f);
            for (int i = 0; i < deep_biomes.Count; i++)
            {
                if (num >= deep_biomes[i].weight_range.X && num < deep_biomes[i].weight_range.Y)
                {
                    return deep_biomes[i];
                }
            }

            return null;
        }

        public static Biome GetSkyBiome(int x, int y)
        {
            var num = biome_noise.GetNoise2D(x, y) * (SkyMaxWeight / 2f);
            for (int i = 0; i < sky_biomes.Count; i++)
            {
                if (num >= sky_biomes[i].weight_range.X && num < sky_biomes[i].weight_range.Y)
                {
                    return sky_biomes[i];
                }
            }

            return null;
        }

        public static BaseBiome GetBiomeAsName(string name)
        {
            var land = prim_landbiomes.Find(B => B.name == name);
            if (land != null) return land;
            var sky = sky_biomes.Find(B => B.name == name);
            if (sky != null) return sky;
            var deep = deep_biomes.Find(B => B.name == name);
            if (deep != null) return deep;
            return null;
        }

        static void RegBiomes()
        {
            //森林
            Register(new ForestBiome());
            //平原
            //Register(new PlainBiome());
            //雪地
            //Register(new SnowfieldBiome());
            //地下通用群系
            Register(new DeepLayerBiome());
            //天空通用群系
            Register(new SkyLayerBiome());
            //
            //Register(new MountainsBiome());
        }

        static BiomeManage()
        {
            RegBiomes();
            ReSetWeight();
        }
    }
}