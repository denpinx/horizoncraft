using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.Struct;
using static horizoncraft.script.WorldControl.BiomeManage;
using Vector2 = Godot.Vector2;

namespace horizoncraft.script.WorldControl
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

        public virtual void GeneratorOre(BiomeStructContext lbsc)
        {
            int trycount = lbsc.Random.Next(16);
            for (int i = 0; i < trycount; i++)
            {
                Vector2I pos = new Vector2I(lbsc.Random.Next(Chunk.Size), lbsc.Random.Next(Chunk.Size));
                var gp = new Vector2I(lbsc.GlobalX + pos.X, lbsc.GlobalY + pos.Y);
                if (lbsc.Random.Next(32) == 1)
                {
                    BlockStruct blockStrcut =
                        StructManage.GetStruct("ore_struct", gp.X, gp.Y, 1, lbsc.Random,
                            "coal_ore", 4);
                    lbsc.BlockStructs.Add(blockStrcut);
                }
                else if (lbsc.Random.Next(48) == 1)
                {
                    BlockStruct blockStrcut =
                        StructManage.GetStruct("ore_struct", gp.X, gp.Y, 1, lbsc.Random,
                            "iron_ore", 3);
                    lbsc.BlockStructs.Add(blockStrcut);
                }
                else if (lbsc.Random.Next(48) == 1)
                {
                    BlockStruct blockStrcut =
                        StructManage.GetStruct("ore_struct", gp.X, gp.Y, 1, lbsc.Random,
                            "copper_ore", 3);
                    lbsc.BlockStructs.Add(blockStrcut);
                }
                else if (lbsc.Random.Next(48) == 1)
                {
                    BlockStruct blockStrcut =
                        StructManage.GetStruct("ore_struct", gp.X, gp.Y, 1, lbsc.Random,
                            "tin_ore", 3);
                    lbsc.BlockStructs.Add(blockStrcut);
                }
            }
        }
    }
}