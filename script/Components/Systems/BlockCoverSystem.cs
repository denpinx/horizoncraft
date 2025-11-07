using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;
/// <summary>
/// 方块覆盖系统
/// 当当前方块被顶部被覆盖时自动转换方块。
/// </summary>
public class BlockCoverSystem : TickSystem
{
    public override void BlockTick(BlockTickEvent e, Component cmp)
    {
        var ec = cmp as ExpandComponent;
        if (e.CheckIsCube(e.GetTopBlock())) e.SetBlock(Materials.Valueof(ec.BlockName));
    }
}