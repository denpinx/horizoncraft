using Godot;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events;

public class PlayerRightClickBlockEvent:WorldEvent
{
    public Vector3I Position;
    public BlockData blockData;
}