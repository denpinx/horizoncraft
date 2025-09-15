using System.Collections.Generic;

namespace horizoncraft.script.Components.Systems;

public class InventorySystem : TickSystem
{
    public override void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        var inv = component as InventoryComponent;
        if (inv == null) return;
        foreach (var key in value.Keys)
        {
            switch (key)
            {
                case "InventoryTile":
                    inv.InventoryTile = (string)value[key];
                    break;
                case "Action":
                    if (value[key] == "sort")
                        inv.GetInventory().Sort();

                    break;
            }
        }
    }
}