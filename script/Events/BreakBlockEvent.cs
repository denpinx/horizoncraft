using Horizoncraft.script.Inventory;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Events
{
    public class BreakBlockEvent : WorldEvent
    {
        //传入
        public BlockData BlockData;
        public ItemStack ItemStack;

        public int Index;

        //传出
        public ItemStack DropItemStack;
    }
}