using Horizoncraft.script.Components;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Net;
using Horizoncraft.script.Recipes;
using BlockComponents_InventoryComponent = Horizoncraft.script.Components.BlockComponents.InventoryComponent;
using InventoryComponent = Horizoncraft.script.Components.BlockComponents.InventoryComponent;

namespace Horizoncraft.tscn.Menu.Inventory;

public partial class CraftingTableInventory : InventoryNode
{
    private InvSlot ResultItem;

    public override void _Ready()
    {
        TargetNodeCount = 9;
        TargetNodePath = "VBoxContainer/PanelContainer_Craft/HBoxContainer/GridContainer/InvSlot";
        PlayerNodeCount = 36;
        PlayerNodePath = "VBoxContainer/PanelContainer_Inv/GridContainer/PlayerInvSlot";
        base._Ready();
        ResultItem =
            GetNode<InvSlot>(
                "VBoxContainer/PanelContainer_Craft/HBoxContainer/InvSlot_result");
        ResultItem.LeftClick += OnCraftButtonPressed;
        ResultItem.RightClick += OnCraftButtonPressed;
    }

    public void OnCraftButtonPressed(int index, bool isShit)
    {
        if (PlayerNode?.world == null) return;
        
        //本地兼远程方法,如果当前是本地或则服务端则自动调用本地方法，如果当前是客户端则调用客户端方法，修改组件内容
        var scd = new SetComponentData();
        if (isShit) scd.AddComponentSet("WorkBenchComponent", "Action", "Craft-All");
        else scd.AddComponentSet("WorkBenchComponent", "Action", "Craft");
        var sobc = new PlayerSetBlockComponentEvent()
        {
            world = PlayerNode.world,
            Player = PlayerNode.playerData,
            ComponentData = scd
        };
        PlayerNode.world.Service.PlayerService.Events.SetOpenBlockComponent(sobc);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        //检查合成结果
        if (PlayerNode?.world == null) return;

        var inv = TargetBlock?.GetComponent<BlockComponents_InventoryComponent>()?.GetInventory();
        if (inv == null) return;

        var gri = RecipeManage.GetRecipe(inv, 3);
        ResultItem.SetShowItem(gri?.Result);
    }   
}