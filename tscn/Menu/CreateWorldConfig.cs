using Godot;
using System;
using horizoncraft.script;

public partial class CreateWorldConfig : Control
{
    [Export]private LineEdit lineEditWorldName, lineEditWorldSeed;
    [Export]private TextureButton buttonCreateWorld, buttonCancel;

    public override void _Ready()
    {
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