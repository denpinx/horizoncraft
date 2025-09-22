using Godot;
using System;
using horizoncraft.script;

public partial class CreateWorldConfig : Control
{
    private LineEdit lineEditWorldName, lineEditWorldSeed;
    private TextureButton buttonCreateWorld, buttonCancel;

    public override void _Ready()
    {
        lineEditWorldName =
            GetNode<LineEdit>("HBoxContainer/VBoxContainer/PanelContainer/VBoxContainer/LineEdit_WorldName");
        lineEditWorldSeed =
            GetNode<LineEdit>("HBoxContainer/VBoxContainer/PanelContainer/VBoxContainer/LineEdit_WorldSeed");
        buttonCreateWorld =
            GetNode<TextureButton>(
                "HBoxContainer/VBoxContainer/PanelContainer/VBoxContainer/HBoxContainer/Button_CreateWorld");
        buttonCancel =
            GetNode<TextureButton>(
                "HBoxContainer/VBoxContainer/PanelContainer/VBoxContainer/HBoxContainer/Button_Cancel");

        buttonCancel.Pressed += () => { QueueFree(); };
        buttonCreateWorld.Pressed += () =>
        {
            World.worldMode = World.WorldMode.Single;
            World.WorldName = lineEditWorldName.Text;

            if (lineEditWorldSeed.Text == "") World.Seed = System.Random.Shared.Next();
            else if (int.TryParse(lineEditWorldSeed.Text, out int seed))
                World.Seed = seed;
            else World.Seed = lineEditWorldSeed.Text.Hash();

            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
            QueueFree();
        };
    }
}