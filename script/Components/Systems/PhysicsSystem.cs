using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class PhysicsSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, Component cmp)
    {
        PhysicsComponent pc = cmp as PhysicsComponent;
        BlockMeta pcmeta = Materials.Valueof(pc.BlockName);
        BlockMeta air = Materials.Valueof("air");
        if (e.CheckCanReplaceAndNotMeta(e.GetBottomBlock(),pcmeta))
        {
            e.DropBlockLoot(e.GetBottomBlock());
            e.SetBottomBlock(pcmeta, 0);
            e.SetBlock(air);
            return;
        }
        else if (e.CheckMeta(e.GetTopBlock(), pcmeta))
        {
            if (e.CheckCanReplaceAndNotMeta(e.GetLeftBlock(),pcmeta))
            {
                e.DropBlockLoot(e.GetLeftBlock());
                e.SetLeftBlock(pcmeta, 0);
                e.SetBlock(air);
                return;
            }
            else if (e.CheckCanReplaceAndNotMeta(e.GetRightBlock(),pcmeta))
            {
                e.DropBlockLoot(e.GetRightBlock());
                e.SetRightBlock(pcmeta, 0);
                e.SetBlock(air);
                return;
            }
        }
    }
}