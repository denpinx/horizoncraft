using System;
using horizoncraft.script.Components.BlockComponents;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class CropGrowthSystem:TickSystem
{
    private static BlockMeta farmland = Materials.Valueof("farmland");
    public override void Ticking(BlockTickEvent e, Component cmp)
    {
        if (cmp is CropGrowComponent cgc)
        {
            if (e.BlockData.State >= cgc.GrowState - 1)
            {
                return;
            }
            
            if (cgc.Water)
            {
                var bottom = e.GetBottomBlock();
                if (!e.CheckMeta(bottom, farmland)||bottom.State==0)
                    return;
            }

            if (Random.Shared.NextSingle() < cgc.GrowChance)
            {
                cgc.GrowValue++;
            }
            
            if (cgc.GrowValue >= cgc.GrowCount)
            {
                cgc.GrowValue = 0;
                e.BlockData.State += 1;
            }
        }
    }
}