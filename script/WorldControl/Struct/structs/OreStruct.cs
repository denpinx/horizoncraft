using System;
using Godot;
using Godot.NativeInterop;

namespace horizoncraft.script.WorldControl.Struct.structs;

public class OreStruct : StructBuild
{
    public OreStruct()
    {
        Name = "ore_struct";
    }

    public override BlockStruct DynamicBuild(int x, int y, int z, Random rand, params object[] args)
    {
        BlockStruct blocks = new BlockStruct();
        if (args.Length < 2)
        {
            GD.PrintErr($"{nameof(OreStruct)} DynamicBuild args.Length < 2 ");
        }

        ;
        string name = (string)args[0];
        int chance = (int)args[1];
        if (z == 1) AddOre(rand, blocks,ref  chance, name, x, y, z);
        return blocks;
    }

    public void AddOre(Random random, BlockStruct blocks, ref int chance, string name, int x, int y, int z)
    {
        if (chance == 0) return;
        if (random.Next(2) == 1)
            blocks.AddBlock(x, y, z, Materials.Valueof(name));
        if (--chance > 0) AddOre(random, blocks, ref chance, name, x - 1, y, 1);
        else return;
        if (--chance > 0) AddOre(random, blocks, ref chance, name, x + 1, y, 1);
        else return;
        if (--chance > 0) AddOre(random, blocks, ref chance, name, x, y - 1, 1);
        else return;
        if (--chance > 0) AddOre(random, blocks, ref chance, name, x, y + 1, 1);
        else return;
    }
}