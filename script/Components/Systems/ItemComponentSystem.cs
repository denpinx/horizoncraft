using System.Collections.Generic;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Components.Systems;

public class ItemComponentSystem : IComponentSystem
{
    public bool Execute(WorldEvent worldEvent, Component component)
    {


        return true;
    }

    public bool Execute(PlayerEvent playerEvent, Component component)
    {
        if (component is ItemComponent ic)
        {
            if (playerEvent is PlayerBreakblockEvent bbe)
            {
                return OnBreakBlock(bbe, ic);
            }

            if (playerEvent is PlayerPlaceBlockEvent pbe)
            {
                return OnPlaceBlock(pbe, ic);
            }
        }

        return true;
    }

    public virtual void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        
    }

    public virtual bool OnBreakBlock(PlayerBreakblockEvent bbe, ItemComponent itemComponent)
    {
        return true;
    }

    public virtual bool OnPlaceBlock(PlayerPlaceBlockEvent pbe, ItemComponent itemComponent)
    {
        return true;
    }
}