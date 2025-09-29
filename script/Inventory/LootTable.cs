using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;

namespace horizoncraft.script.Inventory;

/// <summary>
/// 战利品表
/// </summary>
public class LootTable
{
    /// <summary>
    /// 战利品列表
    /// </summary>
    public List<LootItem> LootItems = new();

    /// <summary>
    /// 尝试获取战利品
    /// </summary>
    /// <param name="luck">幸运值，影响掉落几率，默认为1</param>
    /// <returns>获取的战利品</returns>
    public List<ItemStack> TryTakeItem(int state,float luck = 1f)
    {
        var loots = new List<ItemStack>();
        foreach (LootItem item in LootItems)
        {
            if (item.DropState != -1)
                if(item.DropState != state)continue;
            
            var chance = System.Random.Shared.NextSingle();
            if (item.DropChance * luck >= chance)
            {
                var amount = 0;

                foreach (var ac in item.AmountChances)
                {
                    GD.Print(ac.Amount, ac.Chance);
                    var amount_chance = System.Random.Shared.NextSingle();
                    if (ac.Chance * luck >= amount_chance)
                        amount += ac.Amount;
                }

                if (amount > 0)
                    loots.Add(item.Item.Copy(amount));
            }
        }

        return loots;
    }
}