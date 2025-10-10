using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Utility;
using horizoncraft.script.WorldControl.Struct.structs;

namespace horizoncraft.script.WorldControl.Struct;

public class StructManage
{
    public static List<StructBuild> DynamicStructs = new();
    public static Dictionary<string, StaticBuildStruct> StaticBuildStructs = new();
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

    public static void LoadStaticBuilds()
    {
        List<string> list = [];
        DirUtility.GetAllFiles("config/builds", list);
        foreach (var dir in list)
        {
            if (!dir.EndsWith(".json")) continue;
            FileAccess file = FileAccess.Open(dir, FileAccess.ModeFlags.Read);
            var dict = JsonCleaner.FromJson(file.GetAsText());
            if (dict.TryGetValue("name", out var name))
            {
                StaticBuildStruct staticBuildStruct = new StaticBuildStruct();
                staticBuildStruct.Name = name as string;
                if (dict.TryGetValue("blocks", out var blocks))
                {
                    var block_lsit = blocks as List<object>;
                    foreach (var blockdata in block_lsit)
                    {
                        var block_dict = blockdata as Dictionary<string, object>;
                        if (Materials.BlockMetas.TryGetValue((string)block_dict["name"], out var meta))
                        {
                            BlockStructItem block_itme = new();
                            block_itme.Coord.X = (int)block_dict["x"];
                            block_itme.Coord.Y = (int)block_dict["y"];
                            block_itme.Coord.Z = (int)block_dict["z"];
                            block_itme.BlockMeta = meta;
                            staticBuildStruct.blockStructItems.Add(block_itme.Coord, block_itme);
                        }
                    }

                    StaticBuildStructs.Add(staticBuildStruct.Name, staticBuildStruct);
                    GD.Print($"加载结构: {staticBuildStruct.Name},方块数量:{staticBuildStruct.blockStructItems.Count}");
                    foreach (var block in staticBuildStruct.blockStructItems)
                    {
                        GD.Print($"{block.Key.ToString()} {block.Value.BlockMeta.Name}");
                    }
                }
            }
        }
    }

    static StructManage()
    {
        LoadStaticBuilds();
        RegStructs();
    }
}