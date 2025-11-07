using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.WorldControl.worldbiomes;

namespace horizoncraft.script.WorldControl
{
    public struct BiomeItem
    {
        public Vector2 WeightRange;
        public float Weight;
        public string Name;
    }

    public enum BiomeType
    {
        LandBiome,
        Sky,
        Deep
    }

    public static class BiomeManage
    {
        private static float Zoom = 0.3f;
        private static FastNoiseLite BiomeNoise = new FastNoiseLite();

        private static List<LandBiome> LandBiomes = new List<LandBiome>();
        private static List<BiomeItem> MixinLandbiomes = new List<BiomeItem>();
        private static List<Biome> DeepBiomes = new List<Biome>();
        private static List<Biome> SkyBiomes = new List<Biome>();
        private static float LandMaxWeight = 0;
        private static float MixinLandMaxWeight = 0;
        private static float DeepMaxWeight = 0;
        private static float SkyMaxWeight = 0;

        /// <summary>
        /// 注册生物群系
        /// </summary>
        /// <param name="basebiome">群系类型</param>
        private static void Register(BaseBiome basebiome)
        {
            if (basebiome is LandBiome landBiome)
            {
                LandBiomes.Add(landBiome);
                LandMaxWeight += basebiome.weight;
            }
            else if (basebiome is Biome biome)
            {
                if (biome.biomeType == BiomeType.Deep)
                {
                    DeepMaxWeight += basebiome.weight;
                    DeepBiomes.Add(biome);
                }
                else
                {
                    SkyMaxWeight += basebiome.weight;
                    SkyBiomes.Add(biome);
                }
            }
        }

        /// <summary>
        /// 获取原始地表群系
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static LandBiome GetLandBiome(string name)
        {
            for (int i = 0; i < LandBiomes.Count; i++)
                if (LandBiomes[i].name == name)
                    return LandBiomes[i];
            return null;
        }


        //将群戏放大化，使得权重分配更随机
        private static void Amplification()
        {
            MixinLandbiomes.Clear();
            //步骤一 混合与放大
            // 2-4-8-16-32
            Random random = new Random((int)World.Seed);
            for (int i = 0; i < 5; i++)
                MixinBiomes(random, 32);

            //todo 步骤二, 添加过度群戏

            //步骤三，分配权重
            float MaxWeight = 0;
            for (int i = 0; i < MixinLandbiomes.Count; i++)
                MaxWeight += MixinLandbiomes[i].Weight;

            MixinLandMaxWeight = MaxWeight;
            float half = -MaxWeight / 2;

            for (int i = 0; i < MixinLandbiomes.Count; i++)
            {
                MixinLandbiomes[i] = new BiomeItem()
                {
                    Name = MixinLandbiomes[i].Name,
                    Weight = MixinLandbiomes[i].Weight,
                    WeightRange = new Vector2(half, half + MixinLandbiomes[i].Weight)
                };
                half += MixinLandbiomes[i].Weight;
            }
        }

        /// <summary>
        /// 混淆群系
        /// </summary>
        /// <param name="time">混淆次数</param>
        private static void MixinBiomes(Random random, int time)
        {
            if (MixinLandbiomes.Count == 0)
            {
                //复制两遍
                for (int i = 0; i < LandBiomes.Count; i++)
                {
                    MixinLandbiomes.Add(LandBiomes[i].GetBiomeItem());
                    MixinLandbiomes.Add(LandBiomes[i].GetBiomeItem());
                }
            }
            else
            {
                //自我复制一遍
                var max = MixinLandbiomes.Count;
                for (int i = 0; i < max; i++)
                    MixinLandbiomes.Add(MixinLandbiomes[i]);
            }

            for (int i = 0; i < time; i++)
            {
                int v1 = random.Next(MixinLandbiomes.Count);
                int v2 = random.Next(MixinLandbiomes.Count);
                if (v1 != v2)
                {
                    (MixinLandbiomes[v1], MixinLandbiomes[v2]) = (MixinLandbiomes[v2], MixinLandbiomes[v1]);
                }
            }
        }

        /// <summary>
        /// 更新原始群系权重
        /// </summary>
        private static void ReSetWeight()
        {
            float half = -LandMaxWeight / 2;
            for (int i = 0; i < LandBiomes.Count; i++)
            {
                LandBiomes[i].weight_range.X = half;
                LandBiomes[i].weight_range.Y = half + LandBiomes[i].weight;
                half += LandBiomes[i].weight;
                GD.Print(
                    $"群系名称：{LandBiomes[i].name} 权重:{LandBiomes[i].weight}，{LandBiomes[i].weight_range.X},{LandBiomes[i].weight_range.Y}");
            }

            half = -DeepMaxWeight / 2;
            for (int i = 0; i < DeepBiomes.Count; i++)
            {
                DeepBiomes[i].weight_range.X = half;
                DeepBiomes[i].weight_range.Y = half + DeepBiomes[i].weight;
                half += DeepBiomes[i].weight;
                GD.Print(
                    $"群系名称：{DeepBiomes[i].name} 权重:{DeepBiomes[i].weight}，{DeepBiomes[i].weight_range.X},{DeepBiomes[i].weight_range.Y}");
            }

            half = -SkyMaxWeight / 2;
            for (int i = 0; i < SkyBiomes.Count; i++)
            {
                SkyBiomes[i].weight_range.X = half;
                SkyBiomes[i].weight_range.Y = half + SkyBiomes[i].weight;
                half += SkyBiomes[i].weight;
                GD.Print(
                    $"群系名称：{SkyBiomes[i].name} 权重:{SkyBiomes[i].weight}，{SkyBiomes[i].weight_range.X},{SkyBiomes[i].weight_range.Y}");
            }
        }

        /// <summary>
        /// 检查给予的高度图和区块坐标，是否属于一维群系，还是二维群系
        /// </summary>
        /// <param name="HighMap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取混淆后的地表群系
        /// </summary>
        /// <param name="x">一维坐标</param>
        /// <returns></returns>
        public static LandBiome GetMixinLandBiome(int x)
        {
            var num = BiomeNoise.GetNoise1D((float)x * Zoom) * ((float)MixinLandMaxWeight / 2f);
            for (int i = 0; i < MixinLandbiomes.Count; i++)
            {
                if (num >= MixinLandbiomes[i].WeightRange.X && num < MixinLandbiomes[i].WeightRange.Y)
                {
                    return GetLandBiome(MixinLandbiomes[i].Name);
                }
            }

            return null;
        }

        /// <summary>
        /// 获取地下的二维生物群系
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Biome GetDeepBiome(int x, int y)
        {
            var num = BiomeNoise.GetNoise2D(x * Zoom, y * Zoom) * (DeepMaxWeight / 2f);
            for (int i = 0; i < DeepBiomes.Count; i++)
            {
                if (num >= DeepBiomes[i].weight_range.X && num < DeepBiomes[i].weight_range.Y)
                {
                    return DeepBiomes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 获取地上的二维生物群系
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Biome GetSkyBiome(int x, int y)
        {
            var num = BiomeNoise.GetNoise2D(x * Zoom, y * Zoom) * (SkyMaxWeight / 2f);
            for (int i = 0; i < SkyBiomes.Count; i++)
            {
                if (num >= SkyBiomes[i].weight_range.X && num < SkyBiomes[i].weight_range.Y)
                {
                    return SkyBiomes[i];
                }
            }

            return null;
        }

        public static BaseBiome GetBiomeAsName(string name)
        {
            var land = LandBiomes.Find(B => B.name == name);
            if (land != null) return land;
            var sky = SkyBiomes.Find(B => B.name == name);
            if (sky != null) return sky;
            var deep = DeepBiomes.Find(B => B.name == name);
            if (deep != null) return deep;
            GD.PrintErr($"没有找到生物群系{name}");
            return null;
        }

        static void RegBiomes()
        {
            //森林
            Register(new ForestBiome());
            //平原
            Register(new PlainBiome());
            //雪地
            Register(new SnowfieldBiome());
            //地下通用群系
            Register(new DeepLayerBiome());
            //天空通用群系
            Register(new SkyLayerBiome());
            //
            //Register(new MountainsBiome());
        }

        public static void Reset()
        {
            BiomeNoise.Seed = (int)World.Seed;
            Amplification();
        }

        static BiomeManage()
        {
            BiomeNoise.Seed = (int)World.Seed;
            RegBiomes();
            ReSetWeight();
            Amplification();
        }
    }
}