using Godot;
using System;
using horizoncraft.script;
using horizoncraft.script.I18N;
using HorizonCraft.script.Services.chunk;

/// <summary>
/// 物品管理器
/// TODO 待完成
/// </summary>
public partial class ItemManage : HBoxContainer, ITranslatable
{
    private int Page = 0;
    private int PageItemMax = 10;

    private Button _buttonMode0;
    private Button _buttonMode1;
    private Button _buttonClear;
    private Button _buttonSort;
    private Button _buttonLight0;
    private Button _buttonLight1;
    private Button _buttonLight2;

    public override void _Ready()
    {
        _buttonMode0 = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer/Button_mod_0");
        _buttonMode1 = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer/Button_mod_1");
        _buttonClear = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer2/Button_clear");
        _buttonSort = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer2/Button_sort");
        _buttonLight0 = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer3/Button_Light_0");
        _buttonLight1 = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer3/Button_Light_1");
        _buttonLight2 = (Button)GetNode("PanelContainer2/VBoxContainer/HBoxContainer3/Button_Light_2");

        _buttonMode0.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.playerData.Mode = 0;
            }
        };

        _buttonMode1.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.playerData.Mode = 1;
            }
        };

        _buttonClear.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.playerData.Inventory.Clear();
            }
        };

        _buttonSort.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.playerData.Inventory.Sort();
            }
        };

        _buttonLight0.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.world.Service.ChunkService.LightMode = ChunkServiceBase.LightModeEnum.None;
            }
        };

        _buttonLight1.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.world.Service.ChunkService.LightMode = ChunkServiceBase.LightModeEnum.RayCastMode;
            }
        };

        _buttonLight2.Pressed += () =>
        {
            var player = GetParent<InventoryNode>().PlayerNode;
            if (player != null && player.playerData != null)
            {
                player.world.Service.ChunkService.LightMode = ChunkServiceBase.LightModeEnum.DFSMode;
            }
        };

        PackedScene ps = GD.Load<PackedScene>("res://tscn/Menu/InvSlot.tscn");
        var node = GetNode<GridContainer>("PanelContainer/VBoxContainer/GridContainer");
        for (int i = 0; i < Materials.ItemMetas.Count; i++)
        {
            var invslot = ps.Instantiate<InvSlot>();
            invslot.index = i;
            invslot.HideBackGround = true;
            invslot.LeftClick += (index, isleft, isshift) =>
            {
                GD.Print($"[作弊模式] {index} ,{isleft},{isshift}");
                var player = GetParent<InventoryNode>().PlayerNode;
                if (player != null && player.playerData != null)
                {
                    var item = Materials.ItemMetas[index].GetItemStack();
                    if (isshift) item.Amount = 64;
                    player.playerData.Inventory.TryAddItem(item);
                }
            };

            node.AddChild(invslot);

            invslot.SetShowItem(Materials.ItemMetas[i].GetItemStack());
        }
    }

    public void ChangePage(int page)
    {
    }

    public void TranslateChange()
    {
        _buttonMode0.Text = "button_mode_0".Trprefix("ui");
        _buttonMode1.Text = "button_mode_1".Trprefix("ui");
        _buttonSort.Text = "button_inventory_sort".Trprefix("ui");
        _buttonClear.Text = "button_inventory_clear".Trprefix("ui");
    }
}