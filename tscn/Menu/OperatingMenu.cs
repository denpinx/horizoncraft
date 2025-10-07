using Godot;
using System;
using horizoncraft.script;

public partial class OperatingMenu : Control
{
    [Export] private Button Button_Continue;
    [Export] private Button Button_Setting;
    [Export] private Button Button_Exit;

    public override void _Ready()
    {
        Button_Continue.Pressed += QueueFree;
        Button_Setting.Pressed += () => { QueueFree(); };
        Button_Exit.Pressed += () => { GetTree().ChangeSceneToFile("res://tscn/Menu/MainMenu.tscn"); };
    }
}