using System.Linq;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.ComponentState;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems.Reactive;

public class IronTankSystem : TickSystem
{
    public override bool OnRightClick(PlayerRightClickBlockEvent playerRightClickBlockEvent, Component component)
    {
        var inventory = playerRightClickBlockEvent.Player.Inventory;
        var item = inventory.GetToolBarItem();
        if (item == null) return true;

        if (item.Name == "iron_bukkit")
        {
            if (component is IStorage<Fluid> getter)
            {
                var storage = getter.GetStorage();
                if (storage.TryTakeFluid("", 1000, out var result))
                {
                    if (Materials.ItemMetas.TryGetValue($"iron_bukkit_{result.Name}", out var meta))
                    {
                        var resultItem = meta.GetItemStack();
                        if (inventory.TryAddItem(resultItem))
                        {
                            item.Amount -= 1;
                            return false;
                        }
                    }

                    //背包空间不够或没有物品形态，再放回去
                    storage.TryInput(result);
                }
            }

            return true;
        }

        var str = item.GetItemMeta().GetTag("fluid");
        if (str != null)
        {
            if (component is IStorage<Fluid> getter)
            {
                var fluid = new Fluid()
                {
                    Name = str,
                    Amount = 1000,
                };
                if (getter.GetStorage().TryInput(fluid))
                {
                    var used = item.GetItemMeta().GetTag("on-used");
                    if (used != null)
                    {
                        var itemMeta = Materials.ItemMetas[used];
                        if (itemMeta != null)
                        {
                            var resultItem = itemMeta.GetItemStack();
                            if (inventory.TryAddItem(resultItem))
                            {
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }

                    item.Amount -= 1;
                    playerRightClickBlockEvent.UpdateBlock();
                    return false;
                }
            }
        }

        return true;
    }

    public override void ReactiveTick(BlockTickEvent blockTickEvent, ReactiveComponent component)
    {
        if (component is IStorage<Fluid> getter)
        {
            var storage = getter.GetStorage();
            if (storage.States.Count == 0) return;

            var state = storage.States.First();
            //更新状态
            blockTickEvent.BlockData.State = (int)(((float)state.Value.Amount / (float)state.Max) * 8f);

            if (storage.IsEmpty())
            {
                //没有流体，进入待机模式,直到被触发输入流体
            }
            else
            {
                //有流体，通知周围的方块更新,因为有些自动化方块都是靠被动触发的
                blockTickEvent.UpdateNeighborBlock(true);
            }
        }
    }
}