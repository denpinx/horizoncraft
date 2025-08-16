using Godot;
using horizoncraft.script;
using horizoncraft.script.WorldControl;
using System;

public partial class MainMenu : World
{
    private Button ButtonSingle, ButtonHost, ButtonConnect;
    private TextEdit TextEdit;

    public override void _Ready()
    {
        worldMode = WorldMode.Preview;
        base._Ready();
        
        ButtonSingle = GetNode<Button>("CanvasLayer2/Button_Single");
        ButtonHost = GetNode<Button>("CanvasLayer2/Button_Host");
        ButtonConnect = GetNode<Button>("CanvasLayer2/Button_Connect");
        TextEdit = GetNode<TextEdit>("CanvasLayer2/TextEdit");

        ButtonSingle.Pressed += () =>
        {
            worldMode = WorldMode.Single;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        ButtonHost.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerHost;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        ButtonConnect.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerClient;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        TextEdit.TextChanged += () => { Player.LocalName = TextEdit.Text; };
        Player.LocalName = "玩家" + new Random().Next();
    }

    public override void _PhysicsProcess(double delta)
    {
        player.Visible = false;
        player.Inputable = false;
        base._PhysicsProcess(delta);
        player.Position += Vector2.Left * 2;
        if(TextEdit!=null)TextEdit.PlaceholderText = Player.LocalName;
    }
}