using System.Collections.Generic;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Components
{
    public class TickSystem : IComponentSystem
    {
        public bool Execute(WorldEvent worldEvent, Component component)
        {
            if (component is TickComponent tc)
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
            else if (component is InventoryComponent ic)
                ProcessTick(worldEvent as BlockTickEvent, ic);

            return true;
        }

        public bool Execute(PlayerEvent playerEvent, Component component)
        {
            return true;
        }

        public virtual void SetComponentValue(PlayerData player,Component component, Dictionary<string, string> value)
        {
        }

        public virtual void Ticking(BlockTickEvent evnet, Component component)
        {
        }

        public virtual void ProcessTick(BlockTickEvent evnet, InventoryComponent component)
        {
        }
    }
}