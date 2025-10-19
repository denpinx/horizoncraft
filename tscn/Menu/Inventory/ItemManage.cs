using Godot;
using System;
using horizoncraft.script;
using horizoncraft.script.I18N;
using horizoncraft.script.Recipes;
using HorizonCraft.script.Services.chunk;

/// <summary>
/// 物品管理器
/// 已完成基本功能
/// </summary>
public partial class ItemManage : HBoxContainer, ITranslatable
{
    private PackedScene _packedSceneInvSlot = GD.Load<PackedScene>("res://tscn/Menu/InvSlot.tscn");
    private PlayerNode _player;
    [Export] private GridContainer _GridContainer;
    [Export] private Button _buttonMode0;
    [Export] private Button _buttonMode1;
    [Export] private Button _buttonClear;
    [Export] private Button _buttonSort;
    [Export] private Button _buttonLight0;
    [Export] private Button _buttonLight1;
    [Export] private Button _buttonLight2;

    [Export] private Button _buttonHungerClear;
    [Export] private Button _buttonHealthClear;

    [Export] private Button _NextPage;
    [Export] private Button _LastPage;
    [Export] private LineEdit _LineEdit_FilterText;
    [Export] private ReciperView _RecipeView;
    private int _page_Columnh = 16;
    private int _page = 0;
    private string FilterText = "";

    public override void _Ready()
    {
        _player = GetParent<InventoryNode>().PlayerNode;
        _RecipeView.ItemManage = this;
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
        _NextPage.Pressed += () =>
        {
            int sindex = _page + 1 * 5 * _page_Columnh;
            if (sindex < Materials.ItemMetas.Count)
            {
                _page++;
                UpdatePage();
            }
        };
        _LastPage.Pressed += () =>
        {
            int sindex = _page - 1 * 5 * _page_Columnh;
            if (sindex >= 0)
            {
                _page--;
                UpdatePage();
            }
        };
        _LineEdit_FilterText.TextChanged += (text) =>
        {
            FilterText = _LineEdit_FilterText.Text;
            UpdatePage();
        };

        _buttonHungerClear.Pressed += () => { _player.playerData.Hunger.Value = 0; };
        _buttonHealthClear.Pressed += () => { _player.playerData.Health.Value = 0; };

        _LineEdit_FilterText.FocusEntered += () => _player.Inputable = false;
        _LineEdit_FilterText.FocusExited += () => _player.Inputable = true;
        UpdatePage();
    }

    public void UpdatePage()
    {
        foreach (var node in _GridContainer.GetChildren())
            node.QueueFree();

        int start = _page * 5 * _page_Columnh;
        int passindex = 0;
        foreach (var itemMeta in Materials.ItemMetas.Values)
        {
            if (FilterText != "")
            {
                //标签匹配
                if (FilterText.IndexOf('#') == 0)
                {
                    if (!FilterText.Contains(itemMeta.GetTag("thesaurus")))
                    {
                        continue;
                    }
                }
                else if (!itemMeta.Name.Contains(FilterText))
                {
                    continue;
                }
            }


            if (passindex >= start && start < Materials.ItemMetas.Count)
            {
                var invslot = _packedSceneInvSlot.Instantiate<InvSlot>();
                invslot.index = start;
                invslot.HideBackGround = true;
                invslot.LeftClick += (index, isleft, isshift) =>
                {
                    if (_player != null && _player.playerData != null && _player.playerData.Mode == 1 &&
                        invslot.ShowItem != null)
                    {
                        var item = invslot.ShowItem.Copy();
                        if (isshift) item.Amount = 64;
                        _player.playerData.Inventory.TryAddItem(item);
                    }
                    else
                    {
                        if (invslot.ShowItem != null)
                        {
                            _RecipeView.SetPerviewRecipePack(RecipeManage.SearchRecipeBySource(invslot.ShowItem));
                        }
                    }
                };
                invslot.RightClick += (index, isleft, isshift) =>
                {
                    if (invslot.ShowItem != null)
                    {
                        _RecipeView.SetPerviewRecipePack(RecipeManage.SearchRecipeByUsefor(invslot.ShowItem));
                    }
                };

                invslot.Ready += () => invslot.SetShowItem(itemMeta.GetItemStack());
                _GridContainer.AddChild(invslot);
                start++;
            }

            passindex++;
        }
    }

    public void SetSameLevelNodeVisible(bool Visible)
    {
        var parent = GetParent();
        foreach (var node in parent.GetChildren())
        {
            if (node != this && node is CanvasItem ci)
                ci.Visible = Visible;
        }
    }

    public void TranslateChange()
    {
        _buttonMode0.Text = "button_mode_0".Trprefix("ui");
        _buttonMode1.Text = "button_mode_1".Trprefix("ui");
        _buttonSort.Text = "button_inventory_sort".Trprefix("ui");
        _buttonClear.Text = "button_inventory_clear".Trprefix("ui");
    }
}