using System.Collections.Generic;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Events;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Events.SystemEvents;

namespace Horizoncraft.script.Components.Systems.ItemSystems;

public class ItemComponentSystem : ComponentSystem
{
    public override bool ExecuteItemComponent(PlayerEvent playerEvent, Component component)
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