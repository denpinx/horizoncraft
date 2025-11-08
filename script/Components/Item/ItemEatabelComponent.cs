using Horizoncraft.script.Components.Interfaces;
using Horizoncraft.script.I18N;
using MemoryPack;

namespace Horizoncraft.script.Components.Item;

[MemoryPackable]
public partial class ItemEatableComponent : ItemUsefulComponent, ITip
{
    public float Hunger;

    public string GetTip()
    {
        return "item_component_tip_eatable".Trprefix("ui", Hunger) + "\n";
    }
}