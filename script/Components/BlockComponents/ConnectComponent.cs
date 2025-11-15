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
}

public enum MathResult
{
    None,
    Same,
    Input,
    Output
}