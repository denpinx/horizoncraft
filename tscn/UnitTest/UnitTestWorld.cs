using Godot;
using System;
using horizoncraft.script;
using HorizonCraft.script.Services.world;

/// <summary>
/// 用于单元测试的世界
/// </summary>
public partial class UnitTestWorld : World
{
    public override void _Ready()
    {
        Service = new SingleWorldService(this);
    }
}