using Godot;
using Horizoncraft.script.Components;
using FurnaceComponent = Horizoncraft.script.Components.BlockComponents.FurnaceComponent;

public partial class FurnaceInv : InventoryNode
{
    private TextureProgressBar _progressbarProgress;
    private TextureProgressBar _progressbarFuel;

    public override void _Ready()
    {
        TargetNodeCount = 3;
        TargetNodePaths.Add("MarginContainer/VBoxContainer/TargetInvBase/VBoxContainer/Input");
        TargetNodePaths.Add("MarginContainer/VBoxContainer/TargetInvBase/VBoxContainer/Fuel");
        TargetNodePaths.Add("MarginContainer/VBoxContainer/TargetInvBase/VBoxContainer2/OutPut");

        PlayerNodeCount = 36;
        PlayerNodePath = "MarginContainer/VBoxContainer/GridContainer/PlayerInvSlot";

        _progressbarProgress = (TextureProgressBar)GetNode("MarginContainer/VBoxContainer/TargetInvBase/ProgressBar");
        _progressbarFuel =
            (TextureProgressBar)GetNode(
                "MarginContainer/VBoxContainer/TargetInvBase/VBoxContainer/TextureProgressBar_Fuel");
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        //if (TargetBlock == null) return;

        var cmp = TargetBlock?.GetComponent<FurnaceComponent>();
        if (cmp == null) return;

        _progressbarProgress.MaxValue = cmp.ProcessTick;
        _progressbarProgress.Value = cmp.Progress;
        _progressbarFuel.MaxValue = cmp.FuelMax;
        _progressbarFuel.Value = cmp.Fuel;
    }
}