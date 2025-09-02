using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class FluidSystem : TickSystem
{
    const int FluidLenght = 16;

    public override void Ticking(BlockTickEvent e, Component cmp)
    {
        FluidComponent fc = cmp as FluidComponent;
        BlockMeta blockMeta = Materials.Valueof(fc.BlockName);
        //四周蔓延衰减
        if (e.CheckMeta(e.GetBottomBlock(), Materials.Valueof("air")))
        {
            e.SetBottomBlock(blockMeta, 0);
            e.GetBottomBlock().GetComponent<FluidComponent>("FluidComponent").mobility = true;
            return;
        }
        else if (e.CheckMeta(e.GetBottomBlock(), blockMeta))
        {
            e.GetBottomBlock().State = 0;
        }
        else
        {
            if (e.CheckMeta(e.GetLeftBlock(), Materials.Valueof("air")) && e.Blockdata.State < FluidLenght - 1)
            {
                e.SetLeftBlock(blockMeta, e.Blockdata.State + 1);
                e.GetLeftBlock().GetComponent<FluidComponent>("FluidComponent").mobility = true;
                return;
            }

            if (e.CheckMeta(e.GetRightBlock(), Materials.Valueof("air")) && e.Blockdata.State < FluidLenght - 1)
            {
                e.SetRightBlock(blockMeta, e.Blockdata.State + 1);
                e.GetRightBlock().GetComponent<FluidComponent>("FluidComponent").mobility = true;
                return;
            }
        }

        //顶部没有方块
        if (!e.CheckMeta(e.GetTopBlock(), blockMeta))
        {
            if (fc.mobility)
            {
                if (e.Blockdata.State == 0)
                {
                    e.Blockdata.SetMeta("air");
                    return;
                }
                else
                {
                    if ((!e.CheckMeta(e.GetLeftBlock(), blockMeta) || (e.CheckMeta(e.GetLeftBlock(), blockMeta) &&
                                                                       e.GetLeftBlock().State > e.Blockdata.State &&
                                                                       e.GetLeftBlock().State > 0)) &&
                        (!e.CheckMeta(e.GetRightBlock(), blockMeta) || (e.CheckMeta(e.GetRightBlock(), blockMeta) &&
                                                                        e.GetRightBlock().State > e.Blockdata.State &
                                                                        e.GetRightBlock().State > 0))
                       )
                    {
                        e.Blockdata.SetMeta("air");
                        return;
                    }


                    if (e.GetLeftBlock() != null && e.CheckMeta(e.GetLeftBlock(), blockMeta) &&
                        e.GetLeftBlock().State < e.Blockdata.State - 2 &&
                        e.Blockdata.State <= FluidLenght - 1)
                    {
                        e.Blockdata.State = e.GetLeftBlock().State + 1;
                    }

                    if (e.GetRightBlock() != null && e.CheckMeta(e.GetRightBlock(), blockMeta) &&
                        e.GetRightBlock().State < e.Blockdata.State - 2 &&
                        e.Blockdata.State <= FluidLenght - 1)
                    {
                        e.Blockdata.State = e.GetRightBlock().State + 1;
                    }
                }
            }
        }
    }
}