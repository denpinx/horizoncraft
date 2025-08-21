using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class PhysicsSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, TickComponent cmp)
    {
        PhysicsComponent pc = cmp as PhysicsComponent;
        BlockMeta pcmeta = Materials.Valueof(pc.BlockName);
        BlockMeta air = Materials.Valueof("air");
        if (e.CheckMeta(e.BottomBlock, air))
        {
            e.SetBottomBlock(pcmeta, 0);
            e.SetBlockMeta = air;
            return;
        }
        else if (e.CheckMeta(e.TopBlock, pcmeta))
        {
            if (e.CheckMeta(e.LeftBlock, air))
            {
                e.SetLeftBlock(pcmeta, 0);
                e.SetBlockMeta = air;
                return;
            }
            else if (e.CheckMeta(e.RightBlock, air))
            {
                e.SetRightBlock(pcmeta, 0);
                e.SetBlockMeta = air;
                return;
            }
        }
    }
}