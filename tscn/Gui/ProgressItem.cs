using Godot;
using System;

public partial class ProgressItem : PanelContainer
{
    public string name;
    public int value;
    public int max;
    public TextureProgressBar ProgressBar;
    public Label Label;

    public override void _Ready()
    {
        ProgressBar = GetNode<TextureProgressBar>("TextureProgressBar");
        Label = GetNode<Label>("Label");
    }

    public override void _Process(double delta)
    {
        ProgressBar.Value = value;
        ProgressBar.MaxValue = max;
        if (name != "")
        {
            Visible = true;
            Label.Text = $"{name}:{value}/{max}";
        }
        else Visible = false;
    }
}