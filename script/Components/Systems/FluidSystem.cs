using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class FluidSystem : TickSystem
{
    const int FluidLenght = 16;
    BlockMeta air = Materials.Valueof("air");

    public override void Ticking(BlockTickEvent e, Component cmp)
    {
        FluidComponent fc = cmp as FluidComponent;
        BlockMeta blockMeta = Materials.Valueof(fc.BlockName);
        //四周蔓延衰减
        if (e.CheckCanReplaceAndNotMeta(e.GetBottomBlock(), blockMeta))
        {
            e.DropBlockLoot(e.GetBottomBlock());
            e.SetBottomBlock(blockMeta, 0);
            e.GetBottomBlock().GetComponent<FluidComponent>("FluidComponent").mobility = true;
            return;
        }

        if (e.CheckMeta(e.GetBottomBlock(), blockMeta))
        {
            e.GetBottomBlock().State = 0;
        }
        else
        {
            if (e.CheckCanReplaceAndNotMeta(e.GetLeftBlock(), blockMeta) && e.BlockData.State < FluidLenght - 1)
            {
                e.DropBlockLoot(e.GetLeftBlock());
                e.SetLeftBlock(blockMeta, e.BlockData.State + 1);
                e.GetLeftBlock().GetComponent<FluidComponent>("FluidComponent").mobility = true;
                return;
            }

            if (e.CheckCanReplaceAndNotMeta(e.GetRightBlock(), blockMeta) && e.BlockData.State < FluidLenght - 1)
            {
                e.DropBlockLoot(e.GetRightBlock());
                e.SetRightBlock(blockMeta, e.BlockData.State + 1);
                e.GetRightBlock().GetComponent<FluidComponent>("FluidComponent").mobility = true;
                return;
            }
        }

        //顶部没有方块
        if (e.GetTopBlock() != null && !e.CheckMeta(e.GetTopBlock(), blockMeta))
        {
            if (fc.mobility)
            {
                if (e.BlockData.State == 0)
                {
                    e.BlockData.SetMeta("air");
                    return;
                }
                else
                {
                    if ((!e.CheckMeta(e.GetLeftBlock(), blockMeta) || (e.CheckMeta(e.GetLeftBlock(), blockMeta) &&
                                                                       e.GetLeftBlock().State > e.BlockData.State &&
                                                                       e.GetLeftBlock().State > 0)) &&
                        (!e.CheckMeta(e.GetRightBlock(), blockMeta) || (e.CheckMeta(e.GetRightBlock(), blockMeta) &&
                                                                        e.GetRightBlock().State > e.BlockData.State &
                                                                        e.GetRightBlock().State > 0))
                       )
                    {
                        e.BlockData.SetMeta(air);
                        return;
                    }


                    if (e.GetLeftBlock() != null && e.CheckMeta(e.GetLeftBlock(), blockMeta) &&
                        e.GetLeftBlock().State < e.BlockData.State - 2 &&
                        e.BlockData.State <= FluidLenght - 1)
                    {
                        e.BlockData.State = e.GetLeftBlock().State + 1;
                    }

                    if (e.GetRightBlock() != null && e.CheckMeta(e.GetRightBlock(), blockMeta) &&
                        e.GetRightBlock().State < e.BlockData.State - 2 &&
                        e.BlockData.State <= FluidLenght - 1)
                    {
                        e.BlockData.State = e.GetRightBlock().State + 1;
                    }
                }
            }
        }
    }
}