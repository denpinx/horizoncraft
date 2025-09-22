using Godot;

namespace horizoncraft.script.resource;

[GlobalClass]
public partial class BlockStateSetResource : Resource
{
    [Export] public bool Tscn = false;
    [Export] public string TextureName;
}