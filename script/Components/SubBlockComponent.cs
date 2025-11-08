using Godot;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;

namespace Horizoncraft.script.Components;

[MemoryPackable]
public partial class SubBlockComponent : Component
{
    public Vector2 BelongCoord;

    public void SetBelongCoord(Vector2I coord)
    {
        BelongCoord = new Vector2(coord.X, coord.Y);
    }
}