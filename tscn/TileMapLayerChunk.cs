using Godot;
using Horizoncraft.script;
using Horizoncraft.script.WorldControl;

public partial class TileMapLayerChunk : Node2D
{
    [Export] public bool DEBUG = true;
    private World world;
    public Chunk chunk;
    public PlayerNode PlayerNode;
    [Export] TileMapLayer tileMapLayer_font;
    [Export] TileMapLayer tileMapLayer_back;
    [Export] ColorRect shadowRect;
    [Export] DebugView debugView;   

    [Export] RenderNode BackGroundDraw_Layer_0;
    [Export] RenderNode BackGroundDraw_Layer_1;

    private ImageTexture _blockTexture;
    private ShaderMaterial _shadowMaterial;

    private const int CUBE_MASK = 0;
    private const int BG_MASK = 1;

    public override void _Ready()
    {
        world = (World)GetParent();
        debugView.neoBiomeManage = world.Service.NeoWorldGenerator.NeoBiomeManage;
        var result = Materials.CreateTileSet();
        tileMapLayer_font.TileSet = result;
        tileMapLayer_back.TileSet = result;

        _shadowMaterial = shadowRect.Material.Duplicate() as ShaderMaterial;
        shadowRect.Material = _shadowMaterial;
        UpdateShadowRectSize();
    }

    private void UpdateShadowRectSize()
    {
        shadowRect.Size = new Vector2(Chunk.Size * 16, Chunk.Size * 16);
    }

    public void SetChunk(Chunk chunk)
    {
        if (this.chunk == null || this.chunk != chunk)
        {
            chunk.TileMapFullUpdate = true;
        }

        this.chunk = chunk;
        BackGroundDraw_Layer_0.SetConfig(chunk, world);
        BackGroundDraw_Layer_1.SetConfig(chunk, world);
    }

    public override void _Process(double delta)
    {
        if (chunk == null && GetParent() == null)
        {
            QueueFree();
            return;
        }

        if (debugView.Visible)
        {
            debugView.chunk = chunk;
            debugView.QueueRedraw();
        }

        if (chunk is { TileMapFullUpdate: true })
        {
            BackGroundDraw_Layer_0.QueueRedraw();
            BackGroundDraw_Layer_1.QueueRedraw();

            var img = Image.CreateEmpty(Chunk.Size, Chunk.Size, false, Image.Format.Rgba8);

            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    BlockData block_font = chunk.GetBlock(x, y, 1);
                    BlockData block_back = chunk.GetBlock(x, y, 0);

                    byte light = (byte)Mathf.Min(block_font.Light, 8);
                    byte fg_cube = (byte)(block_font.BlockMeta.Cube ? 255 : 0);
                    byte bg_solid = (byte)(!block_back.IsMeta("air") ? 255 : 0);
                    img.SetPixel(x, y, new Color(light / 8f, fg_cube / 255f, bg_solid / 255f, 0));

                    for (int z = 0; z < Chunk.SizeZ && z < 2; z++)
                    {
                        var pos = new Vector2I(x, y);
                        BlockData block = chunk.GetBlock(x, y, z);

                        int tile_id = -1;
                        var bts = block.GetBlockTileSet();
                        if (bts != null) tile_id = bts.tile_id;

                        Vector2I coord = new(0, 0);
                        if (block.BlockMeta.TileType == "tile")
                        {
                            int tile_size = block.GetTileSize();
                            coord = new(x % tile_size, y % tile_size);
                        }
                        if (block.BlockMeta.TileType == "atlas")
                        {
                            coord = new(block.State, 0);
                        }
                        if (block.BlockMeta.TileType == "terrain")
                        {
                            var tag = block.BlockMeta.GetTag("link");
                            var global = new Vector3I(chunk.X * Chunk.Size + x, chunk.Y * Chunk.Size + y, z);
                            coord = PlayerNode.world.Service.ChunkService.GetTerrain(global, "link", tag);
                        }

                        if (z == 0 && !block_font.BlockMeta.Cube)
                        {
                            if (block.BlockMeta.Name == "air")
                                tileMapLayer_back.SetCell(pos, -1);
                            else if (bts != null && bts.scene)
                            {
                                if (tileMapLayer_back.GetCellSourceId(pos) != tile_id)
                                    tileMapLayer_back.SetCell(pos, tile_id, Vector2I.Zero, bts.id);
                            }
                            else if (tileMapLayer_back.GetCellAtlasCoords(pos) != coord ||
                                     tileMapLayer_back.GetCellSourceId(pos) != tile_id)
                                tileMapLayer_back.SetCell(pos, tile_id, coord);
                        }
                        else
                        {
                            if (block.BlockMeta.Name == "air")
                                tileMapLayer_font.SetCell(pos, -1);
                            else if (bts != null && bts.scene)
                                tileMapLayer_font.SetCell(pos, tile_id, Vector2I.Zero, bts.id);
                            else tileMapLayer_font.SetCell(pos, tile_id, coord);
                        }
                    }
                }
            }

            _blockTexture = ImageTexture.CreateFromImage(img);
            if (_shadowMaterial != null)
                _shadowMaterial.SetShaderParameter("block_data", _blockTexture);

            chunk.TileMapFullUpdate = false;
        }
    }
}
