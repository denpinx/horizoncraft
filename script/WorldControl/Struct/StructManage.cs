using System;
using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script.WorldControl.Struct.structs;

namespace horizoncraft.script.WorldControl.Struct;

public class StructManage
{
    public static List<StructBuild> DynamicStructs = new();
    public static Dictionary<string, StructBuild> Dictionary_DynamicStructs = new();

    public static BlockStruct GetStruct(string name, int x, int y, int z, Random rand, params object[] args)
    {
        return Dictionary_DynamicStructs[name].DynamicBuild(x, y, z, rand, args);
    }

    public static void AddStruct(StructBuild structBuild)
    {
        DynamicStructs.Add(structBuild);
        Dictionary_DynamicStructs.Add(structBuild.Name, structBuild);
        GD.Print($"创建结构生成器:{structBuild.Name}");
    }

    public static void RegStructs()
    {
        AddStruct(new OakTreeStruct());
        AddStruct(new MegaOakTreeStruct());
        AddStruct(new OreStruct());
    }


    static StructManage()
    {
        RegStructs();
    }
}