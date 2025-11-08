using Godot;

namespace Horizoncraft.script;

public class PlayerActionProcess
{
    public PlayerAction State = PlayerAction.BreakBlock;
    public float FinalTime = 2;
    public float ProcessTime;
    public Vector3I Position;

    public void Reset()
    {
        ProcessTime = 0;
    }
}

public enum PlayerAction
{
    None,
    BreakBlock,
    UseItem
}