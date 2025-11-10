using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Recipes;

namespace Horizoncraft.script.Components.Systems;

/// <summary>
/// 工作台系统
/// 接收玩家的合成指令，消耗物品栏物品合成。
/// </summary>
public class WorkBenchSystem : TickSystem
{
    public override void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        if (component is BlockComponents.InventoryComponent inventoryComponent)
        {
            var inventory = inventoryComponent.GetInventory();
            if (value.ContainsKey("Action"))
            {
                if (value["Action"] == "Craft-All")
                {
                    GD.Print("Craft-All");
                    var gri = RecipeManage.GetRecipe(inventory, 3, 0, "workbench");
                    while (gri != null)
                    {
                        if (!player.Inventory.TryAddItem(gri.Result.Copy()))
                            return;

                        for (int i = 0; i < 9; i++)
                            inventory.ReduceItemAmount(i);

                        gri = RecipeManage.GetRecipe(inventory, 3);
                    }
                }

                if (value["Action"] == "Craft")
                {
                    GD.Print("Craft");
                    var gri = RecipeManage.GetRecipe(inventory, 3);
                    if (gri != null)
                    {
                        var handitme = player.Inventory.GetHandItemStack();
                        if (handitme == null
                           )
                        {
                            player.Inventory.HandItemStack = gri.Result.Copy();
                        }
                        else if (
                            handitme.Name == gri.Result.Name &&
                            handitme.Amount + gri.Result.Amount <= gri.Result.GetItemMeta().MaxAmount
                        )
                        {
                            handitme.Amount += gri.Result.Amount;
                        }
                        else
                        {
                            return;
                        }

                        for (int i = 0; i < 9; i++)
                        {
                            inventory.ReduceItemAmount(i);
                        }
                    }
                }
            }
        }
    }
}