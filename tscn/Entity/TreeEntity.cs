using Godot;
using horizoncraft.script.Entity;
using System;

public partial class TreeEntity : EntityNode
{
    public override void _Ready()
    {
        Freeze = true;
    }
}
