using MemoryPack;

namespace Horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class ConnectComponent : ReactiveComponent
{
    public AngleState AngleState = new();
}

[MemoryPackable]
public partial class AngleState
{
    public MathResult Up = MathResult.None;
    public MathResult Down = MathResult.None;
    public MathResult Left = MathResult.None;
    public MathResult Right = MathResult.None;

    public bool Same(object obj)
    {
        if (obj is AngleState angleState)
        {
            if (
                angleState.Up != Up ||
                angleState.Down != Down ||
                angleState.Left != Left ||
                angleState.Right != Right
            )
            {
                return false;
            }

            return true;
        }

        return false;
    }

    public AngleState Clone()
    {
        return new AngleState()
        {
            Up = Up,
            Down = Down,
            Left = Left,
            Right = Right,
        };
    }
}

public enum MathResult
{
    None,
    Same,
    Input,
    Output
}