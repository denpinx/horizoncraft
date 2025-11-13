using Godot;
using Horizoncraft.script.Services.world;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.RenderSystem;

public class RenderContext
{
    public required Vector3I Position;
    public required Vector3I GlobalPosition;
    public required Chunk Chunk;
    public required BlockData Block;
    public required Node2D Node;
    public required WorldServiceBase Service;

    public Vector2I pos_v2
    {
        get { return new Vector2I(Position.X, Position.Y); }
    }
}