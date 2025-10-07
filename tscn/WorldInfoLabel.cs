using Godot;
using System;
using horizoncraft.script.I18N;
using horizoncraft.script.Net;

public partial class WorldInfoLabel : HBoxContainer
{
    [Export] Label labelWorldName, labelCreateDate, labelLoadDate, labelWorldSeed;
    [Export] private Button button;
    private bool MoseIn = false;
    public WorldListMenu Parent;
    public Texture2D Noramle, Select;
    public WorldProfile Profile;

    public override void _Ready()
    {
        button.MouseEntered += () => MoseIn = true;
        button.MouseExited += () => MoseIn = false;
        button.Pressed += () => Parent.SelectedWorld = Profile;
    }

    public void SetWorldProfile(WorldProfile profile)
    {
        Profile = profile;
        labelWorldName.Text = profile.WorldName;
        labelCreateDate.Text = "create_date".Trprefix("ui", profile.CreateDate);
        labelLoadDate.Text = "load_date".Trprefix("ui", profile.LoadDate);
        labelWorldSeed.Text = "world_seed".Trprefix("ui", profile.WorldSeed);
    }
}