using System.Collections.Generic;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components
{
    public class TickSystem : IComponentSystem
    {
        public void Execute(WorldEvent worldEvent, Component component)
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
        }

        public virtual void SetComponentValue(Component component, Dictionary<string, string> value)
        {
        }

        public virtual void Ticking(BlockTickEvent evnet, TickComponent component)
        {
        }
    }
}