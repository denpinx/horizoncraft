using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
using horizoncraft.script.Expand;

namespace horizoncraft.script.Components.Systems;

public class ItemEntitySystem : EntitySystem
{
    public override void Tick(EntityTickEvent e)
    {
        var component = e.GetComponent<ItemEntityComponent>();
        if (component.ItemStack == null)
        {
            GD.PrintErr("删除异常物品实体");
            e.WorldService.EntityService.RemoveEntityData(e.UUID);
            return;
        }

        var player = e.WorldService.PlayerService.GetPlayerInRange(e.EntityData.Position.ToVector2I(), 16);
        if (player != null)
        {
            if (player.Inventory.TryAddItem(component.ItemStack))
            {
                e.WorldService.EntityService.RemoveEntityData(e.UUID);
            }
        }

        var itemlist =
            e.WorldService.EntityService.GetEntityInRangeByName(e.EntityData.Position.ToVector2I(), 16, "item_entity");
        if (itemlist == null) return;

        foreach (var entity in itemlist)
        {
            if (entity.Uuid == e.UUID) continue;
            var cmps = entity.GetComponents<ItemEntityComponent>();
            if (cmps == null) continue;
            foreach (var cmp in cmps)
            {
                if (cmp.ItemStack == null) continue;
                if (cmp.ItemStack.Id != component.ItemStack.Id) continue;
                if (component.ItemStack.TryStackItem(cmp.ItemStack).Amount <= 0)
                {
                    e.WorldService.EntityService.RemoveEntityData(entity.Uuid);
                    break;
                }

                if (component.ItemStack.Amount >= cmp.ItemStack.GetItemMeta().MaxAmount)
                    return;
            }
        }
    }
}