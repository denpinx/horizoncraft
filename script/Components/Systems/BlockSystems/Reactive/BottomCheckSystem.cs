using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems.Reactive;

/// <summary>
/// 检查底部是否为指定方块，否则被替换为"空气"
/// </summary>
public class BottomCheckSystem : TickSystem
{
    private BlockMeta _air = Materials.BlockMetas["air"];

    public override void ReactiveTick(BlockTickEvent e, ReactiveComponent component)
    {
        if (!e.CheckIsCube(e.GetBottomBlock()))
        {
            if (component is BottomCheckComponent bcc)
            {
                if (bcc.DropItem)
                {
                    e.DropBlockLoot(e.BlockData);
                }

                e.SetBlock(_air);
                e.UpdateNeighborBlock();
            }
        }
    }
}