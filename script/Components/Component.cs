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
    public abstract partial class Component
    {
        public string Name;
    }
}