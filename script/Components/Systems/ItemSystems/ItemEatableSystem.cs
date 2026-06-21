using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Item;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Utility;
namespace Horizoncraft.script.Components.Systems.ItemSystems;

public class ItemEatableSystem : ItemComponentSystem
{
    public override bool OnItemUse(PlayerUseItemEvent playerUseItemEvent, ItemComponent itemComponent)
    {
        var player = playerUseItemEvent.Player;
        if (itemComponent is ItemEatableComponent iec)
        {
            if (player.Hunger.Value >= player.Hunger.Default)
            {
                GameLogger.Debug("Food","状态已满，不需要食用");
                return false;
            }

            player.Hunger.Value += iec.Hunger;
            playerUseItemEvent.UseItemStack.Amount -= 1;
            GameLogger.Debug("Food","食用物品");
            return true;
        }

        return false;
    }
}