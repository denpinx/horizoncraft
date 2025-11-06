using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Components.EnergyBlocks;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems.BlockSystems.EnergyBlocks;

public class SolarGeneratorSystem : TickSystem
{
    public override void BlockTick(BlockTickEvent blockTickEvent, Component component)
    {
        if (component is EnergyUnitComponent energy)
        {
            int sv = energy.EnergyUnitValue;
            if (blockTickEvent.Service.IsDay())
            {
                if (energy.EnergyUnitValue + energy.Rate <= energy.EnergyUnitMax)
                    energy.EnergyUnitValue += energy.Rate;
                else
                    energy.EnergyUnitValue = energy.EnergyUnitMax;
            }

            if (energy.EnergyUnitValue < energy.Rate) return;

            var result =
                blockTickEvent.Service.ChunkService.GetBlockAsSameComponent<EnergyUnitComponent>(blockTickEvent
                    .GlobalePos);
            foreach (var block in result)
            {
                if (energy.EnergyUnitValue < energy.Rate) return;
                var cmp = block.GetComponent<EnergyUnitComponent>();
                if (cmp == null) continue;
                int space = cmp.EnergyUnitMax - cmp.EnergyUnitValue;
                if (space <= 0) continue;
                if (space > energy.Rate)
                {
                    cmp.EnergyUnitValue += energy.Rate;
                    energy.EnergyUnitValue -= energy.Rate;
                }
                else
                {
                    cmp.EnergyUnitValue = cmp.EnergyUnitMax;
                    energy.EnergyUnitValue -= space;
                }
            }

            if (energy.EnergyUnitValue != sv)
            {
                blockTickEvent.SetUpdate();
            }
        }
    }
}