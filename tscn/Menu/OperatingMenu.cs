using Godot;
using System;
using horizoncraft.script;

public partial class OperatingMenu : Control
{
    public override void _Ready()
    {
        var buttonContinue = GetNode<TextureButton>("HBoxContainer/VBoxContainer/Button_Continue");
        var buttonSetting = GetNode<TextureButton>("HBoxContainer/VBoxContainer/Button_Setting");
        var buttonExit = GetNode<TextureButton>("HBoxContainer/VBoxContainer/Button_Exit");
        buttonContinue.Pressed += QueueFree;
        buttonSetting.Pressed += () => { QueueFree(); };
        buttonExit.Pressed += () => { GetTree().ChangeSceneToFile("res://tscn/Menu/MainMenu.tscn"); };
    }
}