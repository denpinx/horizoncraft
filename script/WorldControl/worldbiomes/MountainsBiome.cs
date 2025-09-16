using System;
using Godot;

namespace horizoncraft.script.WorldControl.worldbiomes;

public class MountainsBiome : LandBiome
{
    public MountainsBiome()
    {
        name = "高山";
        weight = 2;
        DebugColor = Color.Color8(222, 222, 222);
    }

    public override int GetHigh(Random random, FastNoiseLite noise, int x, int z)
    {
        return random.Next(-128, -64);
    }

    public override void GeneratorTerrain(BiomeTerrainContext context)
    {
        int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY; //和当前的插值
        switch (num)
        {
            case 0:
                context.SetBlock("snow");
                break;
            case <= -1:
                context.SetBlock("stone");
                break;
        }
    }
}