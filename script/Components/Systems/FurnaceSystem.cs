using System;
using Godot;
using horizoncraft.script.Events;
using horizoncraft.script.Inventory;
using horizoncraft.script.Recipes;

namespace horizoncraft.script.Components.Systems;

public class FurnaceSystem : TickSystem
{
    public override void ProcessTick(BlockTickEvent evnet, InventoryComponent component)
    {
        FurnaceComponent furnace = component as FurnaceComponent;
        var input = furnace.GetInventory().GetItem(0);
        var fuel = furnace.GetInventory().GetItem(1);
        var output = furnace.GetInventory().GetItem(2);
        bool loadfuel = false;
        if (furnace.Result == null)
        {
            if (input != null)
            {
                var recipe = RecipeManage.GetRecipeItem("furnace",
                    recipeItem => recipeItem.Cost[0].Id == input.Id && recipeItem.Cost[0].Amount <= input.Amount);
                if (recipe != null)
                {
                    loadfuel = furnace.Fuel <= 0;
                    furnace.GetInventory().ReduceItemAmount(0, recipe.Cost[0].Amount);
                    furnace.ProcessTick = recipe.ProcessTick;
                    furnace.Progress = 0;
                    furnace.Result = recipe.Result;
                }
            }
        }
        else
        {
            if (furnace.Progress >= furnace.ProcessTick)
            {
                var result = furnace.Result[0];
                if (output == null)
                {
                    furnace.GetInventory().SetItem(2, result.Copy());
                    furnace.Progress = 0;
                    furnace.Result = null;
                }
                else if (output.Id == result.Id && output.Amount + result.Amount <= result.GetItemMeta().MaxAmount)
                {
                    furnace.GetInventory().AddItemAmount(2, result.Amount);
                    furnace.Progress = 0;
                    furnace.Result = null;
                }
            }

            if (furnace.Progress < furnace.ProcessTick)
                if (furnace.Fuel > 0)
                    furnace.Progress++;
                else loadfuel = true;
        }


        if (furnace.Fuel > 0)
        {
            evnet.Blockdata.State = 1;
            furnace.Fuel--;
        }
        else
        {
            evnet.Blockdata.State = 0;
        }


        if (loadfuel)
            if (furnace.Fuel <= 0 && fuel != null)
            {
                var str = fuel.GetItemMeta().GetTag("fuel");
                if (str != null)
                {
                    furnace.GetInventory().ReduceItemAmount(1);
                    GD.Print($"加油！{str}");
                    int f = str.ToInt();
                    furnace.Fuel += f;
                    furnace.FuelMax = f;
                }
            }
    }
}