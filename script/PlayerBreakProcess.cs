using Godot;

namespace horizoncraft.script;

public class PlayerBreakProcess
{
    public float FinalTime=2;
    public float ProcessTime;
    public Vector3I Position;

    public void Reset()
    {
        ProcessTime = 0;
    }
}