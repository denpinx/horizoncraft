using Godot;
using System;
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
        labelCreateDate.Text = "创建日期:" + profile.CreateDate;
        labelLoadDate.Text = "上次加载日期:" + profile.LoadDate;
        labelWorldSeed.Text = "地图种子:" + profile.WorldSeed.ToString();
    }
}