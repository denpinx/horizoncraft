using Godot;
using horizoncraft.script.Components.EnergyBlocks;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems.BlockSystems.EnergyBlocks;

public class EnergyCableSystem : TickSystem
{
    public override void Ticking(BlockTickEvent blockTickEvent, Component component)
    {
        if (component is EnergyUnitComponent energy)
        {
            if (energy.EnergyUnitValue < energy.Rate) return;
            var result =
                blockTickEvent.Service.ChunkService.GetBlockAsSameComponent<EnergyUnitComponent>(blockTickEvent
                    .GlobalePos);
            foreach (var block in result)
            {
                if (energy.EnergyUnitValue < energy.Rate) return;
                var cmp = block.GetComponent<EnergyUnitComponent>();
                if (cmp == null) continue;
                if(!cmp.InputAble)continue;
                if ((float)cmp.EnergyUnitValue / cmp.EnergyUnitMax >
                    (float)energy.EnergyUnitValue / energy.EnergyUnitMax) continue;

                int space = cmp.EnergyUnitMax - cmp.EnergyUnitValue;
                if (space > energy.Rate)
                {
                    cmp.EnergyUnitValue += energy.Rate;
                    energy.EnergyUnitValue -= energy.Rate;
                }
                else
                {
                    cmp.EnergyUnitValue = energy.EnergyUnitMax;
                    energy.EnergyUnitValue -= space;
                }
            }
        }
    }
}