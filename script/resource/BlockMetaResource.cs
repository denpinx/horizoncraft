using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace horizoncraft.script.resource;

[GlobalClass]
public partial class BlockMetaResource : Resource
{
    public enum TileTypeEnum
    {
        Tile,
        Atlas,
    }

    [ExportGroup("基础属性")] [Export] public string BlockName = "block_name";
    [ExportGroup("基础属性")] [Export] public bool CompleteBlock = true;
    [ExportGroup("基础属性")] [Export] public bool Collide = true;
    [ExportGroup("基础属性")] [Export] public bool LightSource = false;
    [ExportGroup("基础属性")] [Export] public float Rigidity = 0.5f;
    [ExportGroup("基础属性")] [Export] public int BreakLevel = 0;

    [ExportGroup("组件属性")] [Export] public Array<ComponentsResource> Components = null;
    [ExportGroup("高级属性")] [Export] public Array<BlockStateSetResource> BlockStateSets = null;
    [ExportGroup("高级属性")] [Export] public OreConfigResource OreConfig = null;
    [ExportGroup("高级属性")] [Export] public Array<LootItemResource> LootItems = null;
    [ExportGroup("高级属性")] [Export] public TileTypeEnum TileType = TileTypeEnum.Tile;
    [ExportGroup("高级属性")] [Export] public Godot.Collections.Dictionary<string, string> Tags = null;
    [ExportGroup("容器输入遮罩")] [Export] public Array<int> InputMask = null;
    [ExportGroup("容器输出遮罩")] [Export] public Array<int> OutPutMask = null;
}