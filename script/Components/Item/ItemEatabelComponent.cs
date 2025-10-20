using horizoncraft.script.Components.Interfaces;
using horizoncraft.script.I18N;
using MemoryPack;

namespace horizoncraft.script.Components.Item;

[MemoryPackable]
public partial class ItemEatableComponent : ItemUsefulComponent, ITip
{
    public float Hunger;

    public string GetTip()
    {
        return "item_component_tip_eatable".Trprefix("ui", Hunger) + "\n";
    }
}