using MemoryPack;

namespace Horizoncraft.script.ComponentState;

/// <summary>
/// 流体
/// </summary>
[MemoryPackable]
public partial class Fluid : IItem
{
    /// <summary>
    /// 当前流体名
    /// </summary>
    public string Name { get; set; } = "";

    public int Amount { get; set; } = 0;
}