using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Inventory;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events
{
    public class BreakBlockEvent : WorldEvent
    {
        //传入
        public Blockdata Blockdata;
        public ItemStack ItemStack;
        public int Index;
        //传出
        public ItemStack DropItemStack;
    }
}
