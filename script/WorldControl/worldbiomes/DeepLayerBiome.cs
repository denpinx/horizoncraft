using Godot;
using horizoncraft.script.WorldControl.Context;
using horizoncraft.script.WorldControl.Struct;
using horizoncraft.script.WorldControl.Struct.structs;
using static horizoncraft.script.WorldControl.BiomeManage;

namespace horizoncraft.script.WorldControl.worldbiomes
{
    public class DeepLayerBiome : Biome
    {
        public DeepLayerBiome()
        {
            name = "深层";
            biomeType = BiomeType.Deep;
            weight = 100;
            DebugColor = Color.Color8(41, 41, 41);
        }

        public override void GeneratorTerrain(BiomeTerrainContext btc)
        {
            //btc.Chunk[btc.LocalX, btc.LocalY, btc.GloablZ] = Materials.Valueof("stone").Blockdata();
        }

        public override void GeneratorStruct(BiomeStructContext landBiomeStructContext)
        {
            if (landBiomeStructContext.Random.Next(10) == 1)
            {
                if (StructManage.StaticBuildStructs.TryGetValue("mine_mini_room", out StaticBuildStruct staticBuildStruct))
                {
                    Vector3I pos = new Vector3I(
                        landBiomeStructContext.GlobalX + landBiomeStructContext.Random.Next(Chunk.Size),
                        landBiomeStructContext.GlobalY + landBiomeStructContext.Random.Next(Chunk.Size), 0);
                    var build = staticBuildStruct.GetBlockStruct(pos.X, pos.Y, pos.Z);
                    
                    landBiomeStructContext.BlockStructs.Add(build);
                }
                else
                {
                    GD.PrintErr("mine_mini_room 不存在！");
                }
            }
        }
    }
}