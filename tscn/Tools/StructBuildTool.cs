using Godot;
using Godot.Collections;
using Horizoncraft.script;
using Horizoncraft.script.WorldControl.Struct;

/// <summary>
/// 建筑蓝图绘制工具。
/// </summary>
public partial class StructBuildTool : CanvasLayer
{
    PackedScene PackedScene_InvSlot = GD.Load<PackedScene>("res://tscn/Menu/InvSlot.tscn");
    public PreBuildStruct PreBuildStruct = new PreBuildStruct();
    [Export] EditTileMapView EditTileMapView;
    [Export] TextEdit TextEdit;
    [Export] LineEdit LineEdit_BuildName;
    [Export] Button Button_Remove_All;
    [Export] Button Button_Import;
    [Export] Button Button_CreateJson;
    [Export] GridContainer GridContainer;
    public int layer = 0;
    public string SelectedBlock = "stone";

    public override void _Ready()
    {
        _ = Materials.BlockMetas;
        Materials.tileSet = null;

        EditTileMapView.TileSet = Materials.CreateTileSet(true);
        EditTileMapView.BackGround.TileSet = Materials.CreateTileSet(true);

        EditTileMapView.OnSetCell += (pos) =>
        {
            AddBlock(new Vector3I(pos.X, pos.Y, layer), SelectedBlock);
            UpdateTileMap();
        };
        EditTileMapView.OnRemoveCell += (pos) =>
        {
            RemoveBlock(new Vector3I(pos.X, pos.Y, layer));
            UpdateTileMap();
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
        LineEdit_BuildName.TextChanged += (t) => { PreBuildStruct.name = LineEdit_BuildName.Text; };
        Button_Import.Pressed += () =>
        {
            if (DisplayServer.ClipboardHas())
            {
                var json_text = DisplayServer.ClipboardGet();
                Dictionary dict = (Dictionary)Json.ParseString(json_text);

                PreBuildStruct.blocks.Clear();
                PreBuildStruct.ParseDictionary(dict);
                UpdateTileMap();
                GenerateJson();
            }
        };
        Button_CreateJson.Pressed += () => { GenerateJson(); };
        // var GridContainer = GetNode<GridContainer>("HBoxContainer/VBoxContainer3/PanelContainer2/GridContainer");

        int i = 0;
        foreach (var meta in Materials.BlockMetas.Values)
        {
            if (meta.ItemMeta == null) continue;
            var invslot = PackedScene_InvSlot.Instantiate<InvSlot>();
            invslot.index = i;
            invslot.HideBackGround = true;
            invslot.LeftClick += (index, isshift) => { SelectedBlock = meta.Name; };
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
        if (!Materials.BlockMetas.ContainsKey(name)) return;
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