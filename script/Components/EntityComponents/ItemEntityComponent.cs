using Horizoncraft.script.Inventory;
using MemoryPack;
namespace Horizoncraft.script.Components.EntityComponents;

[MemoryPackable]
public partial class ItemEntityComponent : EntityComponent
{
    public ItemStack ItemStack;

    public ItemEntityComponent()
    {
        Drive = "ItemEntityComponent";
    }
}