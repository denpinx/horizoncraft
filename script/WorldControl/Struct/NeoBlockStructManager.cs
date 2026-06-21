using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Utility;
using Horizoncraft.script.WorldControl.Struct.structs;

namespace Horizoncraft.script.WorldControl.Struct;

public class NeoBlockStructManager
{
    /// <summary>
    /// 静态建筑构造器
    /// </summary>
    public readonly Dictionary<string, StaticBuildStruct> StaticBuildStructs = new();

    /// <summary>
    /// 动态建筑构造器
    /// </summary>
    private readonly Dictionary<string, StructBuild> DynamicStructs = new();

    public BlockStruct GetStruct(string name, int x, int y, int z, Random rand, params object[] args)
    {
        if (DynamicStructs.TryGetValue(name, out StructBuild structBuild))
        {
            return structBuild.DynamicBuild(x, y, z, rand, args);
        }

        GameLogger.Error("Struct",$"[{nameof(NeoBlockStructManager)}] 动态结构生成器 {name} 未定义)]");
        return null;
    }

    private void Register(StructBuild structBuild)
    {
        GameLogger.Info("Struct",$"[{nameof(NeoBlockStructManager)}] 注册动态结构生成器: {structBuild.Name,-16} \t #{DynamicStructs.Count,-4}");
        DynamicStructs.Add(structBuild.Name, structBuild);
    }

    void RegStructs()
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

    private void LoadStaticBuilds()
    {
        List<string> list = [];
        DirUtility.GetFiles("res://config/builds", ".json", list);
        foreach (var dir in list)
        {
            FileAccess file = FileAccess.Open(dir, FileAccess.ModeFlags.Read);
            var dict = JsonCleaner.FromJson(file.GetAsText());
            if (dict.TryGetValue("name", out var name))
            {
                StaticBuildStruct BuildStruct = new StaticBuildStruct();
                BuildStruct.Name = name as string;
                if (dict.TryGetValue("blocks", out var blocks))
                {
                    var block_lsit = blocks as List<object>;
                    foreach (var blockdata in block_lsit)
                    {
                        var block_dict = blockdata as Dictionary<string, object>;

                        var data = new BlockStructItem();
                        if (data.ParseDictionary(block_dict))
                        {
                            BuildStruct.blockStructItems.Add(data.Coord, data);
                        }
                    }

                    GameLogger.Info("Struct",
                        $"[{nameof(NeoBlockStructManager)}] 加载建筑结构: {BuildStruct.Name,-16}#{StaticBuildStructs.Count,-14}\t {BuildStruct.blockStructItems.Count,-4}b");
                    StaticBuildStructs.Add(BuildStruct.Name, BuildStruct);
                }
            }
        }
    }

    public void LoadBuilds()
    {
        LoadStaticBuilds();
        RegStructs();
    }
}