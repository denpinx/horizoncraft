using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;
/// <summary>
/// 检查底部是否为指定方块，否则被替换为"空气"
/// </summary>
public class BottomCheckSystem : TickSystem
{
    public override void BlockTick(BlockTickEvent e, Component component)
    {
        if (!e.CheckIsCube(e.GetBottomBlock())) e.SetBlock(Materials.Valueof("air"));
    }
}