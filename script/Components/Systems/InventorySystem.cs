using System.Collections.Generic;
using Horizoncraft.script.Components.BlockComponents;

namespace Horizoncraft.script.Components.Systems;

/// <summary>
/// 物品栏系统
/// </summary>
public class InventorySystem : TickSystem
{
    public override void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        if (component is InventoryComponent inv)
        {
            foreach (var key in value.Keys)
            {
                switch (key)
                {
                    case "InventoryTile":
                        inv.InventoryTile = value[key];
                        break;
                    case "Action":
                        if (value[key] == "sort")
                            inv.GetInventory().Sort();

                        break;
                }
            }
        }
    }
}