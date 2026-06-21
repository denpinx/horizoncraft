using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;
using Horizoncraft.script.Recipes;

namespace Horizoncraft.script.Components.Systems;
/// <summary>
/// 熔炉系统
/// 处理熔炉配方
/// </summary>
public class FurnaceSystem : TickSystem
{
    public NeoRecipeManage NeoRecipeManage
    {
        get
        {
            return csi.WorldService.NeoRecipeManage;
        }
    }

    private ComponentSystemInitialize csi;

    public override void Initialize(ComponentSystemInitialize componentSystemInitialize)
    {
        csi = componentSystemInitialize;
    }

    public override void InventoryTick(BlockTickEvent evnet, InventoryComponent component)
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
                //var recipe = RecipeManage.GetProcessRecipe("furnace",
                   // recipeItem => recipeItem.Cost[0].Id == input.Id && recipeItem.Cost[0].Amount <= input.Amount);
                
                var recipe = NeoRecipeManage.GetProcessRecipe("furnace",[input]);
                
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
                    evnet.UpdateNeighborBlock();
                    furnace.GetInventory().SetItem(2, result.Copy());
                    furnace.Progress = 0;
                    furnace.Result = null;
                }
                else if (output.Name == result.Name && output.Amount + result.Amount <= result.GetItemMeta().MaxAmount)
                {
                    evnet.UpdateNeighborBlock();
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
            evnet.BlockData.State = 1;
            furnace.Fuel--;
        }
        else
        {
            evnet.BlockData.State = 0;
        }


        if (loadfuel)
            if (furnace.Fuel <= 0 && fuel != null)
            {
                var str = fuel.GetItemMeta().GetTag("fuel");
                if (str != null)
                {
                    furnace.GetInventory().ReduceItemAmount(1);
                    int f = str.ToInt();
                    furnace.Fuel += f;
                    furnace.FuelMax = f;
                }
            }
    }
}