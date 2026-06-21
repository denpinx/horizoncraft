using Godot;
using Horizoncraft.script.Components.EntityComponents;
using Horizoncraft.script.Events;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Utility;
namespace Horizoncraft.script.Components.Systems;
/// <summary>
/// 物品实体系统
/// 自动合并相同物品的实体。
/// 当玩家靠经时被玩家拾取。
/// </summary>
public class ItemEntitySystem : EntitySystem
{
    public override void EntityTick(EntityTickEvent e)
    {
        var component = e.GetComponent<ItemEntityComponent>();
        if (component.ItemStack == null)
        {
            GameLogger.Error("Entity","删除异常物品实体");
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

        //合并相同实体
        var itemlist =
            e.WorldService.EntityService.GetEntityInRangeByName(e.EntityData.Position.ToVector2I(), 24, "item_entity");
        if (itemlist == null) return;

        foreach (var entity in itemlist)
        {
            if (entity.Uuid == e.UUID) continue;
            var cmps = entity.GetComponents<ItemEntityComponent>();
            if (cmps == null) continue;
            foreach (var cmp in cmps)
            {
                if (cmp.ItemStack == null) continue;
                if (cmp.ItemStack.Name != component.ItemStack.Name) continue;
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