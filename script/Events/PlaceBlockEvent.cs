using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Events
{
    public class PlaceBlockEvent : WorldEvent
    {
        public ItemStack ItemStack;
        public int Index;
    }
}