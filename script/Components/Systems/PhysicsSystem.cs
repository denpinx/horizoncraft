using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems;

/// <summary>
/// 伪物理系统
/// 模拟沙子物理等。
/// </summary>
public class PhysicsSystem : TickSystem
{
    private readonly BlockMeta _air = Materials.BlockMetas["air"];

    //主动触发
    public override void BlockTick(BlockTickEvent e, Component cmp)
    {
        if (cmp is PhysicsComponent pc)
        {
            if (Materials.BlockMetas.TryGetValue(pc.BlockName, out var pcmeta))
            {
                if (e.CheckCanReplaceAndNotMeta(e.GetBottomBlock(), pcmeta))
                {
                    e.DropBlockLoot(e.GetBottomBlock());
                    e.SetBottomBlock(pcmeta);
                    e.SetBlock(_air);
                    return;
                }

                if (e.CheckMeta(e.GetTopBlock(), pcmeta))
                {
                    if (e.CheckCanReplaceAndNotMeta(e.GetLeftBlock(), pcmeta))
                    {
                        e.DropBlockLoot(e.GetLeftBlock());
                        e.SetLeftBlock(pcmeta);
                        e.SetBlock(_air);
                        return;
                    }

                    if (e.CheckCanReplaceAndNotMeta(e.GetRightBlock(), pcmeta))
                    {
                        e.DropBlockLoot(e.GetRightBlock());
                        e.SetRightBlock(pcmeta);
                        e.SetBlock(_air);
                    }
                }
            }
        }
    }
}