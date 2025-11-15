using Godot;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Events;

public class PlayerRightClickBlockEvent : WorldEvent
{
    public Vector3I Position;
    public BlockData blockData;

    public void UpdateBlock()
    {
        this.World.Service.ChunkService.UpdateBlock(Position);
    }
}