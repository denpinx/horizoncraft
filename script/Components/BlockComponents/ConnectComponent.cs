using MemoryPack;

namespace Horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class ConnectComponent:ReactiveComponent
{
    public AngleState AngleState;
}

[MemoryPackable]
public partial class AngleState
{
    public required MathResult Up=MathResult.None;
    public required MathResult Down=MathResult.None;
    public required MathResult Left=MathResult.None;
    public required MathResult Right=MathResult.None;
}
public enum MathResult
{
    None,
    Same,
    Input,
    Output
}