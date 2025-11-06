using horizoncraft.script.Components;
using horizoncraft.script.Events.player;
using horizoncraft.script.Net;
using horizoncraft.script.Recipes;

namespace HorizonCraft.tscn.Menu.Inventory;

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

        var inv = TargetBlock?.GetComponent<InventoryComponent>()?.GetInventory();
        if (inv == null) return;

        var gri = RecipeManage.GetRecipe(inv, 3);
        ResultItem.SetShowItem(gri?.Result);
    }   
}