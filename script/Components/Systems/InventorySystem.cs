using System.Collections.Generic;
using Godot;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;
/// <summary>
/// 物品栏系统
/// </summary>
public class InventorySystem : TickSystem
{
    // public override void Ticking(BlockTickEvent blockTickEvent, Component component)
    // {
    //     if (component is InventoryComponent box && !box.GetInventory().IsEmpty())
    //     {
    //         GD.Print("not empy");
    //         blockTickEvent.UpdateNeighborBlock();
    //     }
    // }

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