using Godot;
using System;
using horizoncraft.script;

public partial class PlayerSnapshot : CharacterBody2D
{
    [Export]private Label _label;
    [Export]private Sprite2D sprite2D;
    [Export]private AnimationPlayer animation;
    
    private Vector2 LastPosition;
    private double cooldown = 0.25f;
    private bool FaceLeft = true;
    public PlayerData playerData;
    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        animation = GetNode<AnimationPlayer>("AnimationPlayer");
        sprite2D = GetNode<Sprite2D>("Body");
    }

    public override void _Process(double delta)
    {
        if (cooldown > 0)
        {
            cooldown -= delta;
            animation.Play("moving");
        }
        else
        {
            cooldown = 0;
            animation.Play("RESET");
        }

        if (FaceLeft) sprite2D.SetScale(new Vector2(1, 1));
        else sprite2D.SetScale(new Vector2(-1, 1));
    }

    public void SetData(PlayerData pd)
    {
        playerData = pd;
        this._label.Text = pd.Name;
        if (LastPosition != pd.Position_v2)
        {
            cooldown = 0.25f;
        }

        FaceLeft = pd.FaceLeft;
        this.Position = pd.Position_v2;
        LastPosition = Position;
    }
}