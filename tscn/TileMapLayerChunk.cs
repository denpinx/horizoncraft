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
    [Export] TileMapLayer tileMapLayer_shadow;
    [Export] DebugView debugView;

    [Export] RenderNode BackGroundDraw_Layer_0;
    [Export] RenderNode BackGroundDraw_Layer_1;

    //[Export] private float perspectiveOffsetFactor = 0.1f;

    private long time;

    public override void _Ready()
    {
        world = (World)GetParent();
        var result = Materials.CreateTileSet();
        tileMapLayer_font.TileSet = result;
        tileMapLayer_back.TileSet = result;
    }

    public void SetChunk(Chunk chunk)
    {
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

        if (chunk is { update_tilemap: true })
        {
            //调用自定义渲染
            BackGroundDraw_Layer_0.QueueRedraw();
            BackGroundDraw_Layer_1.QueueRedraw();

            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    for (int z = 0; z < Chunk.SizeZ && z < 2; z++)
                    {
                        var pos = new Vector2I(x, y);
                        BlockData block = chunk.GetBlock(x, y, z);
                        BlockData block_back = chunk.GetBlock(x, y, 0);
                        BlockData block_font = chunk.GetBlock(x, y, 1);

                        if (block_font.Light == 0)
                        {
                            if (tileMapLayer_shadow.GetCellAtlasCoords(pos) != Vector2I.Zero)
                                tileMapLayer_shadow.SetCell(pos, 0, new Vector2I(0, 0));
                            if (tileMapLayer_font.GetCellSourceId(pos) != -1)
                                tileMapLayer_font.SetCell(pos, -1);
                            if (tileMapLayer_back.GetCellSourceId(pos) != -1)
                                tileMapLayer_back.SetCell(pos, -1);
                            continue;
                        }

                        
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
                            {
                                tileMapLayer_back.SetCell(new(x, y), -1);
                            }
                            else if (bts != null && bts.scene)
                            {
                                if (tileMapLayer_back.GetCellSourceId(pos) != tile_id)
                                    tileMapLayer_back.SetCell(pos, tile_id, Vector2I.Zero, bts.id);
                            }
                            else if (tileMapLayer_back.GetCellAtlasCoords(pos) != coord ||
                                     tileMapLayer_back.GetCellSourceId(pos) != tile_id)
                                tileMapLayer_back.SetCell(new(x, y), tile_id, coord);
                        }
                        else
                        {
                            if (block_font.Light >= 7)
                            {
                                if (tileMapLayer_shadow.GetCellSourceId(pos) != -1)
                                    tileMapLayer_shadow.SetCell(new(x, y), -1);
                            }
                            else
                            {
                                if (tileMapLayer_shadow.GetCellAtlasCoords(pos) != new Vector2I(block_font.Light, 0))
                                    tileMapLayer_shadow.SetCell(new(x, y), 0, new Vector2I(block_font.Light, 0));
                            }

                            if (block.BlockMeta.Name == "air")
                            {
                                tileMapLayer_font.SetCell(new(x, y), -1);
                            }
                            else if (bts != null && bts.scene)
                                tileMapLayer_font.SetCell(new Vector2I(x, y), tile_id, Vector2I.Zero, bts.id);
                            else tileMapLayer_font.SetCell(new(x, y), tile_id, coord);
                        }
                    }
                }
            }
            
            chunk.update_tilemap = false;
        }
    }
}