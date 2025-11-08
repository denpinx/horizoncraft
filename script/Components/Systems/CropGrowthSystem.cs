using System;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems;
/// <summary>
/// 作物生长系统
/// 每Tick尝试随机生长一次，累计N次后增加一次生长状态。
/// </summary>
public class CropGrowthSystem:TickSystem
{
    private readonly BlockMeta _farmland = Materials.Valueof("farmland");
    public override void BlockTick(BlockTickEvent e, Component cmp)
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
                if (!e.CheckMeta(bottom, _farmland)||bottom.State==0)
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