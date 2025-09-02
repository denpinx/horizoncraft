using horizoncraft.script.Inventory;
using MemoryPack;

namespace horizoncraft.script.Components.EntityComponents;

[MemoryPackable]
public partial class ItemEntityComponent : EntityComponent
{
    public ItemStack ItemStack;
}