using Godot;
using System.Collections.Generic;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Recipes;

public partial class CraftInventory : InventoryNode
{
    private List<InvSlot> CraftSlots = new List<InvSlot>();
    [Export] private InvSlot ResultItem;

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
            invslot.LeftClick += OnPlayerLeftClickItem;
            invslot.RightClick += OnPlayerRightClickItem;
            CraftSlots.Add(invslot);
        }
        ResultItem.LeftClick += OnCraftButtonPressed;
        ResultItem.RightClick += OnCraftButtonPressed;
    }

    private void OnCraftButtonPressed(int index, bool isShift)
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