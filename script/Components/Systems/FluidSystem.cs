using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class FluidSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, Component cmp)
    {
        FluidComponent fc = cmp as FluidComponent;
        BlockMeta blockMeta = Materials.Valueof(fc.BlockName);
        //四周蔓延衰减
        if (e.CheckMeta(e.BottomBlock, Materials.Valueof("air")))
        {
            e.SetBottomBlock(blockMeta, 0);
            e.BottomBlock.GetComponent<FluidComponent>("FluidComponent").mobility = true;
            return;
        }
        else if (e.CheckMeta(e.BottomBlock, blockMeta))
        {
            e.BottomBlock.State = 0;
        }
        else
        {
            if (e.CheckMeta(e.LeftBlock, Materials.Valueof("air")) && e.Blockdata.State < 7)
            {
                e.SetLeftBlock(blockMeta, e.Blockdata.State + 1);
                e.LeftBlock.GetComponent<FluidComponent>("FluidComponent").mobility = true;
                return;
            }

            if (e.CheckMeta(e.RightBlock, Materials.Valueof("air")) && e.Blockdata.State < 7)
            {
                e.SetRightBlock(blockMeta, e.Blockdata.State + 1);
                e.RightBlock.GetComponent<FluidComponent>("FluidComponent").mobility = true;
                return;
            }
        }

        //顶部没有方块
        if (!e.CheckMeta(e.TopBlock, blockMeta))
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
                    if ((!e.CheckMeta(e.LeftBlock, blockMeta) || (e.CheckMeta(e.LeftBlock, blockMeta) &&
                                                                  e.LeftBlock.State > e.Blockdata.State &&
                                                                  e.LeftBlock.State > 0)) &&
                        (!e.CheckMeta(e.RightBlock, blockMeta) || (e.CheckMeta(e.RightBlock, blockMeta) &&
                                                                   e.RightBlock.State > e.Blockdata.State &
                                                                   e.RightBlock.State > 0))
                       )
                    {
                        e.Blockdata.SetMeta("air");
                        return;
                    }


                    if (e.LeftBlock != null && e.CheckMeta(e.LeftBlock, blockMeta) &&
                        e.LeftBlock.State < e.Blockdata.State - 2 &&
                        e.Blockdata.State <= 7)
                    {
                        e.Blockdata.State = e.LeftBlock.State + 1;
                    }

                    if (e.RightBlock != null && e.CheckMeta(e.RightBlock, blockMeta) &&
                        e.RightBlock.State < e.Blockdata.State - 2 &&
                        e.Blockdata.State <= 7)
                    {
                        e.Blockdata.State = e.RightBlock.State + 1;
                    }
                }
            }
        }
    }
}