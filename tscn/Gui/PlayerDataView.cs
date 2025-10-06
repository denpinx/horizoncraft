using Godot;
using System;
using horizoncraft.script;

public partial class PlayerDataView : Control
{
    private PlayerNode Player;
    private TextureProgressBar HealthBar, HungerBar, ThirstBar, ExpBar;
    private Timer _timer;

    public override void _Ready()
    {
        Player = GetParent().GetParent<PlayerNode>();
        _timer = GetNode<Timer>("Timer");
        HealthBar = GetNode<TextureProgressBar>("VBoxContainer/Progress_Health");
        HungerBar = GetNode<TextureProgressBar>("VBoxContainer/Progress_Hunger");
        ThirstBar = GetNode<TextureProgressBar>("VBoxContainer/Progress_Thirst");
        ExpBar = GetNode<TextureProgressBar>("VBoxContainer/Progress_Exp");

        _timer.Timeout += Update;
    }

    public void Update()
    {
        if (Player.playerData == null) return;
        HealthBar.Value = Player.playerData.Health.Value;
        HealthBar.MaxValue = Player.playerData.Health.Default;
        
        HungerBar.Value = Player.playerData.Hunger.Value;
        HungerBar.MaxValue = Player.playerData.Hunger.Default;
    }
}