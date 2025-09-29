using System;
using MemoryPack;
using System.Collections.Generic;
using horizoncraft.script.Components.BlockComponents;
using horizoncraft.script.Components.EnergyBlocks;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Item;

namespace horizoncraft.script.Components
{
    //Data Only
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(TickComponent))]
    [MemoryPackUnion(1, typeof(ExpandComponent))]
    [MemoryPackUnion(2, typeof(FluidComponent))]
    [MemoryPackUnion(3, typeof(PhysicsComponent))]
    [MemoryPackUnion(4, typeof(ItemComponent))]
    [MemoryPackUnion(5, typeof(InventoryComponent))]
    [MemoryPackUnion(6, typeof(FurnaceComponent))]
    [MemoryPackUnion(7, typeof(ItemDurableComponent))]
    [MemoryPackUnion(8, typeof(EntityComponent))]
    [MemoryPackUnion(9, typeof(ItemEntityComponent))]
    [MemoryPackUnion(10, typeof(EnergyUnitComponent))]
    [MemoryPackUnion(11, typeof(ReactiveComponent))]
    [MemoryPackUnion(12, typeof(CropGrowComponent))]
    public abstract partial class Component
    {
        public string Name;
    }
}