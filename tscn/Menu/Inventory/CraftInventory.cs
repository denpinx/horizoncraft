using Godot;
using System;
using System.Collections.Generic;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;
using horizoncraft.script.Recipes;

public partial class CraftInventory : InventoryNode
{
    private List<InvSlot> CraftSlots = new List<InvSlot>();
    private InvSlot ResultItem;

    public override void _Ready()
    {
        PlayerNodeCount = 36;
        PlayerNodePath = "VBoxContainer/PanelContainer_Inv/GridContainer/PlayerInvSlot";
        base._Ready();
        for (int i = 0; i < 4; i++)
        {
            var invslot =
                GetNode<InvSlot>(
                    "VBoxContainer/PanelContainer_Craft/HBoxContainer/GridContainer/InvSlot" + i);
            invslot.index = 36 + i;

            invslot.LeftClick += OnPlayerButtonPressed;
            invslot.RightClick += OnPlayerButtonPressed;
            CraftSlots.Add(invslot);
        }

        ResultItem =
            GetNode<InvSlot>(
                "VBoxContainer/PanelContainer_Craft/HBoxContainer/InvSlot_result");
        ResultItem.LeftClick += OnCraftButtonPressed;
        ResultItem.RightClick += OnCraftButtonPressed;
    }

    public void OnCraftButtonPressed(int index, bool isLeft, bool isShift)
    {
        if (PlayerNode?.world == null) return;
        var cgri = new PlayerCraftItemEvent()
        {
            world = PlayerNode.world,
            Player = PlayerNode.playerData,
            IsAllCraft = isShift
        };
        PlayerNode.world.Service.PlayerService.Events.CraftGridRecipeItem(cgri);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        //检查合成结果

        if (PlayerNode?.world == null) return;

        for (int i = 0; i < 4; i++)
        {
            var invslot = CraftSlots[i];
            var item = PlayerNode.playerData.Inventory.GetItem(36 + i);
            invslot.SetShowItem(item);
        }

        var gri = RecipeManage.GetRecipe(PlayerNode.playerData.Inventory, 2, 36);
        ResultItem.SetShowItem(gri?.Result);
    }
}