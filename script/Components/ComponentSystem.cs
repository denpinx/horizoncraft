using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components
{
    //Function Only
    public interface IComponentSystem
    {
        bool Execute(WorldEvent worldEvent, Component component);
        bool Execute(PlayerEvent playerEvent, Component component);

        void SetComponentValue(PlayerData player,Component component, Dictionary<string, string> value);
    }
}