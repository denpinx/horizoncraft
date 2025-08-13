using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using horizoncraft.script.Events;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components
{
    //Function Only
    public interface IComponentSystem
    {
        void Execute(WorldEvent worldEvent, Component component);
    }
}