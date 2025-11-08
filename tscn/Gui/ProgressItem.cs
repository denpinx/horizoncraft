using Godot;

public partial class ProgressItem : PanelContainer
{
    public string name;
    public int value;
    public int max;
    [Export] public TextureProgressBar ProgressBar;
    [Export] public Label Label;

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