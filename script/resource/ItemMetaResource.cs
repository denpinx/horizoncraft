using Godot;
using Godot.Collections;

namespace horizoncraft.script.resource;

[GlobalClass]
public partial class ItemMetaResource:Resource
{
    public string ItemName;
    public int MaxAmount;
    public string Description;
    public Dictionary<string, string> Tags;
    public Array<string> Textures;
}