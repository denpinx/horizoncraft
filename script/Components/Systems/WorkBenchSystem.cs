using System.Collections.Generic;
using Godot;
using horizoncraft.script.Events;
using horizoncraft.script.Inventory;
using horizoncraft.script.Recipes;

namespace horizoncraft.script.Components.Systems;

public class WorkBenchSystem : TickSystem
{
    public override void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        var BlockInv = (component as InventoryComponent).GetInventory();
        if (value.ContainsKey("Action"))
        {
            if (value["Action"] == "Craft-All")
            {
                GD.Print("Craft-All");
                var gri = RecipeManage.GetRecipe(BlockInv, 3);
                while (gri != null)
                {
                    if (!player.Inventory.TryAddItem(gri.Result.Copy()))
                        return;
                    
                    for (int i = 0; i < 9; i++)
                        BlockInv.ReduceItemAmount(i);
                    
                    gri = RecipeManage.GetRecipe(BlockInv, 3);
                }
            }

            if (value["Action"] == "Craft")
            {
                GD.Print("Craft");
                var gri = RecipeManage.GetRecipe(BlockInv, 3);
                if (gri != null)
                {
                    var handitme = player.Inventory.GetHandItemStack();
                    if (handitme == null
                       )
                    {
                        player.Inventory.HandItemStack = gri.Result.Copy();
                    }
                    else if (
                        handitme.Id == gri.Result.Id &&
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
                        BlockInv.ReduceItemAmount(i);
                    }
                }
            }
        }
    }
}