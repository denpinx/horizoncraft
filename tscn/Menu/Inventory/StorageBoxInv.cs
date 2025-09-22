using Godot;
using System;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Events.player;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl.work;

[Tool]
public partial class StorageBoxInv : InventoryNode
{
    LineEdit lineEdit;
    Button button, button_sort, button3;
    Label label;

    public override void _Ready()
    {
        TargetNodeCount = 36;
        PlayerNodeCount = 36;
        TargetNodePath = "MarginContainer/VBoxContainer/TargetInvBase/TargetInvSlot";
        PlayerNodePath = "MarginContainer/VBoxContainer/GridContainer/PlayerInvSlot";

        base._Ready();

        lineEdit = GetNode<LineEdit>("MarginContainer/VBoxContainer/HBoxContainer/LineEdit");
        button = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/Button");
        button_sort = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/Button2");
        button3 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/Button3");
        label = GetNode<Label>("MarginContainer/VBoxContainer/Label");
        button.Pressed += () =>
        {
            if (PlayerNode != null)
            {
                //这里必须这样写，使用SetComponentData来操控组件，因为涉及到远程调用
                var scd = new SetComponentData();
                //设置标题
                scd.AddComponentSet("BoxComponent", "InventoryTile", lineEdit.Text);
                var sobc = new PlayerSetBlockComponentEvent()
                {
                    world = PlayerNode.world,
                    Player = PlayerNode.playerData,
                    ComponentData = scd,
                };
                PlayerNode.world.Service.PlayerService.Events.SetOpenBlockComponent(sobc);
            }
        };
        button_sort.Pressed += () =>
        {
            if (PlayerNode != null)
            {
                var scd = new SetComponentData();
                //调用方法,排序
                scd.AddComponentSet("BoxComponent", "Action", "sort");
                var sobc = new PlayerSetBlockComponentEvent()
                {
                    world = PlayerNode.world,
                    Player = PlayerNode.playerData,
                    ComponentData = scd,
                };
                PlayerNode.world.Service.PlayerService.Events.SetOpenBlockComponent(sobc);
            }
        };
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (PlayerNode != null)
        {
            if (TargetBlock != null)
            {
                var ic = TargetBlock.GetComponent<InventoryComponent>();
                if (ic != null) label.Text = ic.InventoryTile;
            }
        }
    }
}