using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class PhysicsSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, Component cmp)
    {
        PhysicsComponent pc = cmp as PhysicsComponent;
        BlockMeta pcmeta = Materials.Valueof(pc.BlockName);
        BlockMeta air = Materials.Valueof("air");
        if (e.CheckMeta(e.GetBottomBlock(), air))
        {
            e.SetBottomBlock(pcmeta, 0);
            e.SetBlock(air);
            return;
        }
        else if (e.CheckMeta(e.GetTopBlock(), pcmeta))
        {
            if (e.CheckMeta(e.GetLeftBlock(), air))
            {
                e.SetLeftBlock(pcmeta, 0);
                e.SetBlock(air);
                return;
            }
            else if (e.CheckMeta(e.GetRightBlock(), air))
            {
                e.SetRightBlock(pcmeta, 0);
                e.SetBlock(air);
                return;
            }
        }
    }
}