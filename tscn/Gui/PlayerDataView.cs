using Godot;
using System;
using horizoncraft.script;

public partial class PlayerDataView : Control
{
    private PlayerNode Player;
    [Export]private TextureProgressBar HealthBar, HungerBar, ThirstBar, ExpBar;
    [Export]private Timer _timer;

    public override void _Ready()
    {
        Player = GetParent().GetParent<PlayerNode>();
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