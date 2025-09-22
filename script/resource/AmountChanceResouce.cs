using Godot;

namespace horizoncraft.script.resource;
[GlobalClass]
public partial class AmountChanceResouce : Resource
{
    [Export]public float Chance;
    [Export]public int Amount;
}