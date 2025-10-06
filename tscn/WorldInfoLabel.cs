using Godot;
using System;
using horizoncraft.script.I18N;
using horizoncraft.script.Net;

public partial class WorldInfoLabel : HBoxContainer
{
    Label labelWorldName, labelCreateDate, labelLoadDate, labelWorldSeed;
    private Button button;
    private bool MoseIn = false;
    public WorldListMenu Parent;
    public Texture2D Noramle, Select;
    public WorldProfile Profile;

    public override void _Ready()
    {
        Noramle = GD.Load<Texture2D>("res://texture/Gui/panel6.png");
        Select = GD.Load<Texture2D>("res://texture/Gui/panel7.png");
        button = GetNode<Button>("Button");
        labelWorldName = GetNode<Label>("Button/HBoxContainer/LabelWorldName");
        labelCreateDate = GetNode<Label>("Button/HBoxContainer/LabelCreateDate");
        labelLoadDate = GetNode<Label>("Button/HBoxContainer/LabelLoadDate");
        labelWorldSeed = GetNode<Label>("Button/HBoxContainer/LabelWorldSeed");

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