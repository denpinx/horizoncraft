using Godot;
using Horizoncraft.script;
using Horizoncraft.script.I18N;
using Horizoncraft.script.WorldControl;

public partial class DeadView : Control
{
    [Export] Label Label_Deathrattle;
    [Export] Button Button_Respawn;
    [Export] Button Button_Eixt;
    private PlayerNode Player;

    public override void _Ready()
    {
        Button_Eixt.Pressed += () => { GetTree().ChangeSceneToFile("res://tscn/Menu/MainMenu.tscn"); };
        Button_Respawn.Pressed += () =>
        {
            if (Player == null) return;
            if (Player.playerData == null) return;
            if (Player.playerData.State == PlayerState.Dead)
            {
                Player.playerData.State = PlayerState.Respawning;
                QueueFree();
            }
        };
    }

    public void SetPlayerDead(PlayerNode playerNode)
    {
        Player = playerNode;
        if (playerNode.playerData != null)
        {
            if (playerNode.playerData.Deathrattle == "")
            {
                Label_Deathrattle.Text = "unexpected".Trprefix("dead");
            }
            else
            {
                Label_Deathrattle.Text = playerNode.playerData.Deathrattle.Trprefix("dead");
            }
        }
    }
}