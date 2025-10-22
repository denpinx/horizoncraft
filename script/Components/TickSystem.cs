using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Events.SystemEvents;
using horizoncraft.script.Inventory;
using HorizonCraft.script.Services.world;

namespace horizoncraft.script.Components
{
    public class TickSystem : IComponentSystem
    {
        public bool ExecuteBlockComponent(WorldEvent worldEvent, Component component)
        {
            if (worldEvent is BlockTickEvent)
            {
                if (component is InventoryComponent ic)
                    ProcessTick(worldEvent as BlockTickEvent, ic);
                else if (component is TickComponent tc)
                {
                    if (tc.Current == tc.Max)
                    {
                        Ticking(worldEvent as BlockTickEvent, tc);
                        tc.Current = 0;
                    }
                    else if (tc.Current < tc.Max)
                    {
                        tc.Current++;
                    }
                }
                else if (component is ReactiveComponent rc)
                {
                    GD.Print("ReactiveComponent");
                    ReactiveTick(worldEvent as BlockTickEvent, rc);
                }
            }

            if (worldEvent is PlayerRightClickBlockEvent worldEvent2)
                return OnRightClick(worldEvent2, component);

            return true;
        }

        public bool ExecuteItemComponent(PlayerEvent playerEvent, Component component)
        {
            return true;
        }

        public bool ExecuteEntityComponent(EntitySystemEvent ese)
        {
            foreach (var cmp in ese.EntityData.Components)
            {
                if (cmp is EntityComponent)
                {
                    var e = new EntityTickEvent()
                    {
                        UUID = ese.EntityData.Uuid,
                        WorldService = ese.Service,
                        EntityData = ese.EntityData,
                        EntityComponent = (EntityComponent)cmp,
                    };
                    Tick(e);
                }
            }

            return false;
        }

        public virtual void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
        {
        }

        public virtual void Ticking(BlockTickEvent blockTickEvent, Component component)
        {
        }

        public virtual void ProcessTick(BlockTickEvent blockTickEvent, InventoryComponent component)
        {
        }

        public virtual void Tick(EntityTickEvent e)
        {
        }

        public virtual void ReactiveTick(BlockTickEvent blockTickEvent, ReactiveComponent component)
        {
        }

        public virtual bool OnRightClick(PlayerRightClickBlockEvent playerRightClickBlockEvent, Component component)
        {
            return true;
        }
    }
}