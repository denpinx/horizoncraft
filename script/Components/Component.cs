using System;
using MemoryPack;
using System.Collections.Generic;

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
    public abstract partial class Component
    {
        public enum TickType
        {
            None, //
            Timer, //计时器
        }

        public string Name;
        public virtual TickType Type { get; set; } = TickType.Timer;
    }
}