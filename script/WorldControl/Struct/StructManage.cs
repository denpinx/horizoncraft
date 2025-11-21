using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Utility;
using Horizoncraft.script.WorldControl.Struct.structs;

namespace Horizoncraft.script.WorldControl.Struct;

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
        Register(new TreeBuilder("oak_tree", "oak_log", "oak_leaves")
        {
            RootDeviate = 2,
            RootHigh = new Vector2I(5, 6),
            PeakDeviate = new Vector2I(3, 5),
            PeakHigh = new Vector2I(2, 4),
            LeavesExtend = 0.5f,
        });
        Register(new TreeBuilder("birch_tree", "birch_log", "birch_leaves")
        {
            RootDeviate = 2,
            RootHigh = new Vector2I(5, 6),
            PeakDeviate = new Vector2I(2, 5),
            PeakHigh = new Vector2I(2, 3),
            LeavesExtend = 0.4f,
        });
        Register(new TreeBuilder("spruce_tree", "oak_log", "oak_leaves")
        {
            RootDeviate = 2,
            RootHigh = new Vector2I(6, 12),
            PeakDeviate = new Vector2I(2, 3),
            PeakHigh = new Vector2I(2, 3),
            LeavesExtend = 0.4f,
        });
    }

    private static void LoadStaticBuilds()
    {
        List<string> list = [];
        DirUtility.GetAllFiles("res://config/builds", list);
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