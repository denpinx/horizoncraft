using System;
using System.Collections.Generic;
using Godot;

namespace horizoncraft.script.Inventory;

public class InventoryManage
{
    public static Dictionary<string, Node> Inventorys = new();

    public static T RegInv<T>(string name, T node) where T : Node
    {
        Inventorys.Add(name, node);
        return (T)node;
    }

    public static T GetInventory<T>(string name) where T : CanvasLayer
    {
        if (!Inventorys.ContainsKey(name))
        {
            GD.PrintErr($"容器 {name} 不存在！");
        }

        return (T)Inventorys[name];
    }

    static InventoryManage()
    {
        RegInv("PlayerInventory", GD.Load<PackedScene>("res://tscn/Menu/Inventory/InventoryNode.tscn").Instantiate());
        RegInv("StorageBoxInv", GD.Load<PackedScene>("res://tscn/Menu/Inventory/StorageBoxInv.tscn").Instantiate());
    }
}