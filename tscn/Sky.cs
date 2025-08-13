using Godot;
using System;

public partial class Sky : Node3D
{
    public DirectionalLight3D directionalLight3D;
    public override void _Ready()
    {
        directionalLight3D = GetNode<DirectionalLight3D>("DirectionalLight3D");
    }
}
