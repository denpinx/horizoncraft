using Godot;
using Horizoncraft.script.I18N;
using Horizoncraft.script.Net;

public partial class WorldInfoLabel : HBoxContainer, ITranslatable
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
        TranslateChange();
    }

    public void TranslateChange()
    {
        labelWorldName.Text = Profile.WorldName;
        labelCreateDate.Text = "create_date".Trprefix("ui", Profile.CreateDate);
        labelLoadDate.Text = "load_date".Trprefix("ui", Profile.LoadDate);
        labelWorldSeed.Text = "world_seed".Trprefix("ui", Profile.WorldSeed);
    }
}