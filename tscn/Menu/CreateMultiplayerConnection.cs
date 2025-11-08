using System;
using Godot;
using Horizoncraft.script;
using HorizonCraft.script.Services.world;
namespace HorizonCraft.tscn.Menu;

public partial class CreateMultiplayerConnection : Control
{
    [Export] private LineEdit _lineEditIp;
    [Export] private LineEdit _lineEditPort;
    [Export] private TextureButton _buttonCancel;
    [Export] private TextureButton _buttonCreate;
    private string _worldName = "";
    private long _worldSeed;
    public Action OnChangeScene;

    public override void _Ready()
    {
        _buttonCancel.Pressed += () => { QueueFree(); };
        _buttonCreate.Pressed += () =>
        {
            World.worldMode = World.WorldMode.MultiplayerHost;
            World.WorldName = _worldName;
            World.Seed = _worldSeed;

            HostWorldService.Port = int.Parse(_lineEditPort.Text);
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
            OnChangeScene?.Invoke();
        };
    }

    public void OnCancel()
    {
        QueueFree();
    }

    public void SetWorld(string worldName, long worldSeed)
    {
        this._worldName = worldName;
        this._worldSeed = worldSeed;
    }
}