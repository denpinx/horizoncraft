using System.Collections.Generic;
using horizoncraft.script.Events;
namespace horizoncraft.script.Components
{
    public class TickSystem : IComponentSystem
    {
        public delegate void Ticked(BlockTickEvent evnet, TickComponent component);
        public void Execute(WorldEvent worldEvent, Component component)
        {
            if (component is TickComponent tc)
            {
                if (tc.Current == tc.Max)
                {
                    Tick?.Invoke(worldEvent as BlockTickEvent, tc);
                    tc.Current = 0;
                }
                else
                if (tc.Current < tc.Max)
                {
                    tc.Current++;
                }
            }
        }
        public Ticked Tick;
    }
}