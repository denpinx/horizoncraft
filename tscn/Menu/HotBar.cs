using Godot;
using System;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Inventory;

public partial class HotBar : CanvasLayer
{
    public PlayerNode PlayerNode;
    private List<Slot> slots = new List<Slot>();
    private Timer timer = new Timer();

    public override void _Ready()
    {
        timer = GetNode<Timer>("Timer");
        for (int i = 0; i < 9; i++)
        {
            slots.Add(GetNode<Slot>($"Control/PanelContainer/HBoxContainer/Slot{i + 1}"));
        }

        timer.Timeout += update;
    }

    public void update()
    {
        if (PlayerNode != null && PlayerNode.playerData != null)
        {
            for (int i = 0; i < 9; i++)
            {
                ItemStack item = PlayerNode.playerData.Inventory.GetItem(i);
                slots[i].SetShowItem(item);
                if (PlayerNode.playerData.Inventory.ToolBarIndex == i)
                {
                    slots[i].OnHover();
                }
                else
                {
                    slots[i].OnUnhover();
                }
            }

            PlayerNode.playerData.Inventory.update = false;
        }
    }
}