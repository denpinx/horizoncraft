using Godot;

namespace horizoncraft.script.resource;

[GlobalClass]
public partial class OreConfigResource : Resource
{
    [Export] public int Count = 1;
    [Export] public int Size = 1;
    [Export] public int Deep = 1;
}
