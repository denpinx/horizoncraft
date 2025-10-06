using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using horizoncraft.script;
using horizoncraft.script.I18N;
using horizoncraft.script.Inventory;
using horizoncraft.script.Recipes;

public partial class ReciperView : Control, ITranslatable
{
    private PackedScene _packedSceneInvSlot = GD.Load<PackedScene>("res://tscn/Menu/InvSlot.tscn");


    public Dictionary<String, RecipePack> RecipePacks;
    public RecipePack SelectedRecipePack;

    public int Index = 0;

    [Export] private HBoxContainer _TypeList;
    [Export] private PanelContainer _ProcessPanel;
    [Export] private PanelContainer _GridPanel;
    [Export] private GridContainer _Process_Container;
    [Export] private GridContainer _Process_OutPut_Container;
    [Export] private GridContainer _Grid_Container;
    [Export] private GridContainer _Grid_OutPut_Container;
    [Export] private Button _Button_Next, _Button_Last;
    [Export] private Label _Label;

    public ItemManage ItemManage;

    public override void _Ready()
    {
        _Button_Next.Pressed += () =>
        {
            int count = GetRecipeCount();
            if (Index + 1 < count)
            {
                Index++;
            }
            else Index = 0;

            UpdateView();
        };
        _Button_Last.Pressed += () =>
        {
            if (Index - 1 >= 0)
            {
                Index--;
            }
            else Index = GetRecipeCount() - 1;

            UpdateView();
        };
    }

    public override void _Input(InputEvent @event)
    {
        if ((Input.IsActionJustPressed("e") || Input.IsActionJustPressed("OpenOperatingMenu")) && Visible)
        {
            Visible = false;
            ItemManage.SetSameLevelNodeVisible(true);
        }


        if (Input.IsActionJustReleased("roller_down"))
        {
            int count = GetRecipeCount();
            if (Index + 1 < count)
            {
                Index++;
            }
            else Index = 0;

            UpdateView();
        }

        if (Input.IsActionJustReleased("roller_up"))
        {
            if (Index - 1 >= 0)
            {
                Index--;
            }
            else Index = GetRecipeCount() - 1;

            UpdateView();
        }
    }

    public void SetPerviewRecipePack(Dictionary<String, RecipePack> recipePacks)
    {
        Index = 0;
        if (recipePacks.Count == 0)
        {
            UpdateView();
            Visible = false;
            ItemManage.SetSameLevelNodeVisible(true);
            return;
        }

        ItemManage.SetSameLevelNodeVisible(false);
        Visible = true;
        RecipePacks = recipePacks;

        SelectedRecipePack = recipePacks.First().Value;

        foreach (var node in _TypeList.GetChildren())
            node.QueueFree();

        int i = 0;
        foreach (var recipePack in recipePacks.Values)
        {
            var slot = _packedSceneInvSlot.Instantiate<InvSlot>();
            ItemMeta item = null;
            if (recipePack.Tag == "player") item = Materials.ItemMetas["workbench"];
            else if (Materials.ItemMetas.TryGetValue(recipePack.Tag, out var meta)) item = meta;
            else
            {
                GD.PrintErr("RecipePack Tag not found: " + recipePack.Tag);
            }

            slot.Ready += () =>
                slot.SetShowItem(item.GetItemStack());
            slot.index = i++;
            slot.LeftClick += (index, b, arg3) =>
            {
                var rp = recipePacks.ToArray()[index];
                SelectedRecipePack = rp.Value;
                Index = 0;
                UpdateView();
            };
            _TypeList.AddChild(slot);
        }

        UpdateView();
    }

    public void UpdateView()
    {
        if (SelectedRecipePack is GridRecipePack grp)
        {
            foreach (var node in _Grid_Container.GetChildren())
                node.QueueFree();
            foreach (var node in _Grid_OutPut_Container.GetChildren())
                node.QueueFree();

            var recipe = grp.Recipes[Index];

            ItemStack[,] Items = null;


            int wide = 0;
            int height = 0;

            if (recipe.MatchType == RecipeItemMatchType.ItemMatch)
            {
                wide = recipe.Cost.GetLength(0);
                height = recipe.Cost.GetLength(1);
            }

            if (recipe.MatchType == RecipeItemMatchType.TagMatch)
            {
                wide = recipe.CostTagMatch.GetLength(0);
                height = recipe.CostTagMatch.GetLength(1);
            }

            int Max = wide > height ? wide : height;

            _Grid_Container.Columns = Max;
            Items = new ItemStack[Max, Max];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < wide; x++)
                {
                    if (recipe.MatchType == RecipeItemMatchType.ItemMatch)
                    {
                        Items[x, y] = recipe.Cost[x, y];
                    }

                    if (recipe.MatchType == RecipeItemMatchType.TagMatch)
                    {
                        var meta = Materials.GetItemByTag(recipe.CostTagMatch[x, y]);
                        if (meta != null)
                            Items[x, y] = meta.GetItemStack();
                    }
                }
            }


            _Label.Text = $"{Index + 1}/{grp.Recipes.Count}";
            for (int y = 0; y < Max; y++)
            {
                for (int x = 0; x < Max; x++)
                {
                    var Slot = _packedSceneInvSlot.Instantiate<InvSlot>();
                    Slot.LeftClick = (i, s, b) => LeftClick(Slot, i, s, b);
                    Slot.RightClick = (i, s, b) => RightClick(Slot, i, s, b);
                    Slot.Ready += () => Slot.SetShowItem(Items[x, y]);
                    _Grid_Container.AddChild(Slot);
                }
            }

            var Slot_output = _packedSceneInvSlot.Instantiate<InvSlot>();
            Slot_output.LeftClick = (i, s, b) => LeftClick(Slot_output, i, s, b);
            Slot_output.RightClick = (i, s, b) => RightClick(Slot_output, i, s, b);
            Slot_output.Ready += () => Slot_output.SetShowItem(recipe.Result.Copy());
            _Grid_OutPut_Container.AddChild(Slot_output);


            _GridPanel.Visible = true;
        }
        else
            _GridPanel.Visible = false;

        if (SelectedRecipePack is ProcessRecipePack prp)
        {
            foreach (var node in _Process_Container.GetChildren())
                node.QueueFree();
            foreach (var node in _Process_OutPut_Container.GetChildren())
                node.QueueFree();

            var recipe = prp.Recipes[Index];
            int cost_wide = (int)Math.Sqrt(recipe.Cost.Count);
            if (cost_wide == 0) cost_wide = recipe.Cost.Count;
            _Process_Container.Columns = cost_wide;

            int result_wide = (int)Math.Sqrt(recipe.Result.Count);
            if (result_wide == 0) result_wide = recipe.Cost.Count;
            _Process_OutPut_Container.Columns = result_wide;
            _Label.Text = $"{Index}/{prp.Recipes.Count}";

            for (int i = 0; i < recipe.Cost.Count; i++)
            {
                var Slot = _packedSceneInvSlot.Instantiate<InvSlot>();
                var itemstack = recipe.Cost[i].Copy();
                Slot.LeftClick = (i, s, b) => LeftClick(Slot, i, s, b);
                Slot.RightClick = (i, s, b) => RightClick(Slot, i, s, b);

                Slot.Ready += () => Slot.SetShowItem(itemstack);
                _Process_Container.AddChild(Slot);
            }

            for (int i = 0; i < recipe.Result.Count; i++)
            {
                var Slot = _packedSceneInvSlot.Instantiate<InvSlot>();
                var itemstack = recipe.Result[i].Copy();
                Slot.LeftClick = (i, s, b) => LeftClick(Slot, i, s, b);
                Slot.RightClick = (i, s, b) => RightClick(Slot, i, s, b);
                Slot.Ready += () => Slot.SetShowItem(itemstack);
                _Process_OutPut_Container.AddChild(Slot);
            }

            _ProcessPanel.Visible = true;
        }
        else
            _ProcessPanel.Visible = false;
    }

    public int GetRecipeCount()
    {
        if (SelectedRecipePack is GridRecipePack grp)
        {
            return grp.Recipes.Count;
        }

        if (SelectedRecipePack is ProcessRecipePack prp)
        {
            return prp.Recipes.Count;
        }

        return 0;
    }


    public void LeftClick(InvSlot invslot, int index, bool isleft, bool isshift)
    {
        if (invslot.ShowItem != null)
        {
            SetPerviewRecipePack(RecipeManage.SearchRecipeBySource(invslot.ShowItem));
        }
    }

    public void RightClick(InvSlot invslot, int index, bool isleft, bool isshift)
    {
        if (invslot.ShowItem != null)
        {
            SetPerviewRecipePack(RecipeManage.SearchRecipeByUsefor(invslot.ShowItem));
        }
    }

    public void TranslateChange()
    {
        _Button_Next.Text = "button_recipe_next".Trprefix("ui");
        _Button_Last.Text = "button_recipe_last".Trprefix("ui");
    }
}