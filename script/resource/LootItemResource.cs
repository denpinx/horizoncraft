using Godot;
using Godot.Collections;

namespace horizoncraft.script.resource;

[GlobalClass]
public partial class LootItemResource : Resource
{
    [Export] public string Name;
    [Export] public float DropChance = 1;
    [Export] public Array<AmountChanceResouce> AmountChances;
}