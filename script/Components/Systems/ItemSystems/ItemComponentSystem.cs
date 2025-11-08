using System.Collections.Generic;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Events;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Events.SystemEvents;

namespace HorizonCraft.script.Components.Systems.ItemSystems;

public class ItemComponentSystem : IComponentSystem
{
    public bool ExecuteBlockComponent(WorldEvent worldEvent, Component component)
    {
        return true;
    }

    public bool ExecuteItemComponent(PlayerEvent playerEvent, Component component)
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

            if (playerEvent is PlayerUseItemEvent puie)
            {
                return OnItemUse(puie, ic);
            }
        }

        return true;
    }

    public bool ExecuteEntityComponent(EntitySystemEvent ese)
    {
        return false;
    }

    public virtual void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
    }

    public virtual bool OnBreakBlock(PlayerBreakblockEvent bbe, ItemComponent itemComponent)
    {
        var block = bbe.GetBlockData();
        if (block != null) bbe.DropLoots = (block.BlockMeta.LootTable.TryTakeItem(block.State));
        return true;
    }

    public virtual bool OnPlaceBlock(PlayerPlaceBlockEvent pbe, ItemComponent itemComponent)
    {
        return true;
    }

    public virtual bool OnItemUse(PlayerUseItemEvent playerUseItemEvent, ItemComponent itemComponent)
    {
        return true;
    }
}