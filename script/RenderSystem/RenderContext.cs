using Godot;
using Horizoncraft.script.Services.world;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.RenderSystem;

public class RenderContext
{
    public required Vector2I Position;
    public required Chunk Chunk;
    public required BlockData Block;
    public required Node2D Node;
    public required WorldServiceBase Service;
}