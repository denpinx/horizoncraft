using System;
using Godot;
using Horizoncraft.script.WorldControl.Context;
using Horizoncraft.script.WorldControl.Struct;
using Horizoncraft.script.WorldControl.Struct.structs;

namespace Horizoncraft.script.WorldControl.worldbiomes;

public class DesertBiome : LandBiome
{
    public DesertBiome()
    {
        name = "沙漠";
        weight = 1;
        DebugColor = Color.Color8(255, 205, 165);
    }

    public override int GetHigh(Random random, FastNoiseLite noise, int x, int z)
    {
        return random.Next(-16, 8);
    }

    public override void GeneratorStruct(LandBiomeStructContext landBiomeStructContext)
    {
        if (landBiomeStructContext.Random.Next(400) == 1)
        {
            if (BlockStructManager.StaticBuildStructs.TryGetValue("desert_temple",
                    out StaticBuildStruct staticBuildStruct))
            {
                Vector3I pos = new Vector3I(landBiomeStructContext.GlobalX, landBiomeStructContext.GlobalY, 0);
                var build = staticBuildStruct.GetBlockStruct(pos.X, pos.Y, pos.Z);
                landBiomeStructContext.BlockStructs.Add(build);
            }
            else
            {
                GD.PrintErr("desert_temple 不存在！");
            }
        }
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
                    context.SetBlock("sandstone");
                    break;
                case -3:
                    if (context.Random.Next(2) == 1)
                        context.SetBlock("sandstone");
                    else
                        context.SetBlock("sandstone");
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
                    if (context.Random.Next(3) == 1)
                        context.SetBlock("deadbrush");
                    else if(context.Random.Next(5)==1)
                        context.SetBlock("cactus");
                    
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
                    context.SetBlock("sandstone");
                    break;
                case -4:
                    context.SetBlock("sandstone");
                    break;
                case <= -5:
                    context.SetBlock("stone");
                    break;
            }
        }
    }
}