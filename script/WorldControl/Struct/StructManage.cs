using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Utility;
using horizoncraft.script.WorldControl.Struct.structs;

namespace horizoncraft.script.WorldControl.Struct;

/// <summary>
/// 方块结构管理器
/// </summary>
public static class BlockStructManager
{
    /// <summary>
    /// 静态建筑构造器
    /// </summary>
    public static readonly Dictionary<string, StaticBuildStruct> StaticBuildStructs = new();

    /// <summary>
    /// 动态建筑构造器
    /// </summary>
    private static readonly Dictionary<string, StructBuild> DynamicStructs = new();

    public static BlockStruct GetStruct(string name, int x, int y, int z, Random rand, params object[] args)
    {
        if (DynamicStructs.TryGetValue(name, out StructBuild structBuild))
        {
            return structBuild.DynamicBuild(x, y, z, rand, args);
        }

        GD.PrintErr($"[{nameof(BlockStructManager)}] 动态结构生成器 {name} 未定义)]");
        return null;
    }

    private static void Register(StructBuild structBuild)
    {
        GD.Print($"[{nameof(BlockStructManager)}] 注册动态结构生成器: {structBuild.Name,-16} \t #{DynamicStructs.Count,-4}");
        DynamicStructs.Add(structBuild.Name, structBuild);
    }

    static void RegStructs()
    {
        Register(new OakTreeStruct());
        Register(new MegaOakTreeStruct());
    }

    private static void LoadStaticBuilds()
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

                        var data = new BlockStructItem();
                        if (data.ParseDictionary(block_dict))
                        {
                            staticBuildStruct.blockStructItems.Add(data.Coord, data);
                        }
                        // if (Materials.BlockMetas.TryGetValue((string)block_dict["name"], out var meta))
                        // {
                        //     BlockStructItem block_itme = new();
                        //     block_itme.Coord.X = (int)block_dict["x"];
                        //     block_itme.Coord.Y = (int)block_dict["y"];
                        //     block_itme.Coord.Z = (int)block_dict["z"];
                        //     block_itme.BlockMeta = meta;
                        // }
                    }

                    GD.Print(
                        $"[{nameof(BlockStructManager)}] 加载建筑结构: {staticBuildStruct.Name,-16}#{StaticBuildStructs.Count,-14}\t {staticBuildStruct.blockStructItems.Count,-4}b");
                    StaticBuildStructs.Add(staticBuildStruct.Name, staticBuildStruct);
                }
            }
        }
    }

    static BlockStructManager()
    {
        LoadStaticBuilds();
        RegStructs();
    }
}