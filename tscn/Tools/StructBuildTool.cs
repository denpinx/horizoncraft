using Godot;
using System;
using horizoncraft.script;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Struct;

public partial class StructBuildTool : CanvasLayer
{
    public PreBuildStruct PreBuildStruct = new PreBuildStruct();
    EditTileMapView EditTileMapView;
    TextEdit TextEdit;

    public int layer = 0;
    public string SelectedBlock = "stone";

    public override void _Ready()
    {
        EditTileMapView = GetNode<EditTileMapView>("HBoxContainer/VBoxContainer/PanelContainer/EditTileMapView");
        TextEdit = GetNode<TextEdit>("HBoxContainer/VBoxContainer2/PanelContainer2/TextEdit");
        EditTileMapView.TileSet = Materials.CreateTileSet();

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

        var GridContainer = GetNode<GridContainer>("HBoxContainer/VBoxContainer3/PanelContainer2/GridContainer");
        PackedScene ps = GD.Load<PackedScene>("res://tscn/Menu/InvSlot.tscn");
        int i = 0;
        foreach (var meta in Materials.BlockMetas)
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


    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.E))
            EditTileMapView.Mode = EditTileMapView.PenMode.Draw;
        if (Input.IsKeyPressed(Key.R))
            EditTileMapView.Mode = EditTileMapView.PenMode.Remove;
    }

    public void UpdateTileMap()
    {
        EditTileMapView.Clear();
        foreach (var block in PreBuildStruct.blocks.Values)
        {
            if (block.z <= layer)
            {
                var meta = Materials.Dictionary_BlockMetas[block.name];
                var bts = meta.GetBlockTileSet(0);
                EditTileMapView.SetCell(new(block.x, block.y), bts.tile_id, new Vector2I(0, 0));
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