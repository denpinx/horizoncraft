using Horizoncraft.script.Inventory;

namespace Horizoncraft.script.Events
{
    public class PlaceBlockEvent : WorldEvent
    {
        public ItemStack ItemStack;
        public int Index;
    }
}