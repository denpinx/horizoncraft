using System.Collections.Generic;
using horizoncraft.script.Events;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Components.Systems;

public class ItemComponentSystem : IComponentSystem
{
    public bool Execute(WorldEvent worldEvent, Component component)
    {
        if (component is ItemComponent ic)
        {
            if (worldEvent is BreakBlockEvent bbe)
            {
                return OnBreakBlock(bbe, ic);
            }

            if (worldEvent is PlaceBlockEvent pbe)
            {
                return OnPlaceBlock(pbe, ic);
            }
        }

        return true;
    }

    public virtual void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        
    }

    public virtual bool OnBreakBlock(BreakBlockEvent bbe, ItemComponent itemComponent)
    {
        return true;
    }

    public virtual bool OnPlaceBlock(PlaceBlockEvent pbe, ItemComponent itemComponent)
    {
        return true;
    }
}