using Godot;
using System;
using horizoncraft.script;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Struct;

public partial class StructBuildTool : CanvasLayer
{
    public PreBuildStruct PreBuildStruct = new PreBuildStruct();
    [Export] EditTileMapView EditTileMapView;
    [Export] TextEdit TextEdit;
    [Export] Button Button_Remove_All;
    public int layer = 0;
    public string SelectedBlock = "stone";

    public override void _Ready()
    {
        EditTileMapView = GetNode<EditTileMapView>("HBoxContainer/VBoxContainer/PanelContainer/EditTileMapView");
        TextEdit = GetNode<TextEdit>("HBoxContainer/VBoxContainer2/PanelContainer2/TextEdit");
        EditTileMapView.TileSet = Materials.CreateTileSet();
        EditTileMapView.BackGround.TileSet = Materials.CreateTileSet();

        EditTileMapView.OnSetCell += (pos) =>
        {
            AddBlock(new Vector3I(pos.X, pos.Y, layer), SelectedBlock);
            UpdateTileMap();
            GenerateJson();
        };
        EditTileMapView.OnRemoveCell += (pos) =>
        {
            RemoveBlock(new Vector3I(pos.X, pos.Y, layer));
            UpdateTileMap();
            GenerateJson();
        };
        EditTileMapView.PickCell += (pos) =>
        {
            if (PreBuildStruct.blocks.TryGetValue(new Vector3I(pos.X, pos.Y, layer), out var block))
            {
                SelectedBlock = block.name;
            }
        };
        Button_Remove_All.Pressed += () =>
        {
            PreBuildStruct.blocks.Clear();
            UpdateTileMap();
        };
        var GridContainer = GetNode<GridContainer>("HBoxContainer/VBoxContainer3/PanelContainer2/GridContainer");
        PackedScene ps = GD.Load<PackedScene>("res://tscn/Menu/InvSlot.tscn");
        int i = 0;
        foreach (var meta in Materials.BlockMetas.Values)
        {
            if (meta.ItemMeta == null) continue;
            var invslot = ps.Instantiate<InvSlot>();
            invslot.index = i;
            invslot.HideBackGround = true;
            invslot.LeftClick += (index, isleft, isshift) => { SelectedBlock = meta.Name; };
            GridContainer.AddChild(invslot);
            invslot.SetShowItem(meta.ItemMeta.GetItemStack());
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustReleased("tab"))
        {
            if (layer == 0) layer = 1;
            else layer = 0;

            if (layer == 0)
            {
                EditTileMapView.SelfModulate = Color.Color8(255, 255, 255, 32);
            }
            else
            {
                EditTileMapView.SelfModulate = Color.Color8(255, 255, 255, 255);
            }
            
            EditTileMapView.GetParent<Control>().GrabFocus();
        }
    }

    public override void _Process(double delta)
    {
    }

    public void UpdateTileMap()
    {
        EditTileMapView.Clear();
        EditTileMapView.BackGround.Clear();
        EditTileMapView.MaxPos = PreBuildStruct.GetMaxPos();
        EditTileMapView.MinPos = PreBuildStruct.GetMinPos();
        
        foreach (var block in PreBuildStruct.blocks.Values)
        {
            if (block.z == 1)
            {
                var meta = Materials.BlockMetas[block.name];
                var bts = meta.GetBlockTileSet(0);
                EditTileMapView.SetCell(new(block.x, block.y), bts.tile_id, new Vector2I(0, 0));
            }

            if (block.z == 0)
            {
                var meta = Materials.BlockMetas[block.name];
                var bts = meta.GetBlockTileSet(0);
                EditTileMapView.BackGround.SetCell(new(block.x, block.y), bts.tile_id, new Vector2I(0, 0));
            }
        }
    }

    public void GenerateJson()
    {
        string json = Json.Stringify(PreBuildStruct.ToDictionary(), "\t", true);
        TextEdit.Text = json;
    }

    public void AddBlock(Vector3I pos, string name)
    {
        if (name == "") return;
        if(!Materials.BlockMetas.ContainsKey(name))return;
        if (!PreBuildStruct.blocks.ContainsKey(pos))
        {
            PreBuildStruct.blocks.Add(pos, new PreBuildStructItem()
            {
                name = name,
                x = pos.X,
                y = pos.Y,
                z = pos.Z,
            });
        }
    }

    public void RemoveBlock(Vector3I pos)
    {
        PreBuildStruct.blocks.Remove(pos);
    }
}