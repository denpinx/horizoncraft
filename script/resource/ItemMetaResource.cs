using Godot;
using Godot.Collections;

namespace horizoncraft.script.resource;

[GlobalClass]
public partial class ItemMetaResource : Resource
{
    [ExportGroup("基础属性")] [Export] public string ItemName;
    [ExportGroup("基础属性")] [Export] public int MaxAmount;
    [ExportGroup("基础属性")] [Export] public string Description;
    [ExportGroup("高级属性")] [Export] public Array<ComponentsResource> Components;
    [ExportGroup("高级属性")] [Export] public Dictionary<string, string> Tags;
    [ExportGroup("高级属性")] [Export] public Array<string> Textures;
}