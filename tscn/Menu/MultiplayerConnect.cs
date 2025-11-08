using System;
using System.Net;
using Godot;
using Horizoncraft.script;
using HorizonCraft.script.Services.world;

namespace HorizonCraft.tscn.Menu;

public partial class MultiplayerConnect : Control
{
    [Export] private LineEdit _lineEditIp;
    [Export] private LineEdit _lineEditPort;
    [Export] private TextureButton _buttonCancel;
    [Export] private TextureButton _buttonConnect;
    public Action OnChangeScene;

    public override void _Ready()
    {
        _buttonCancel.Pressed += () => { QueueFree(); };
        _buttonConnect.Pressed += () =>
        {
            var ip = _lineEditIp.Text;
            if (ip == "") ip = "127.0.0.1";
            ClientWorldService.Port = int.Parse(_lineEditPort.Text);
            ClientWorldService.ip = ip;
            World.worldMode = World.WorldMode.MultiplayerClient;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
            OnChangeScene?.Invoke();
        };
    }
}