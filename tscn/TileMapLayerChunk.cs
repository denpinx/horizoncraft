using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Godot;
using horizoncraft.script;
using horizoncraft.script.WorldControl;

public partial class TileMapLayerChunk : Node2D
{
    [Export] public bool DEBUG = true;
    public Chunk chunk;
    public Player player;
    TileMapLayer tileMapLayer_font;
    TileMapLayer tileMapLayer_back;
    TileMapLayer tileMapLayer_shadow;
    DebugView debugView;
    //[Export] private float perspectiveOffsetFactor = 0.1f;
    
    private long time;

    public override void _Ready()
    {
        tileMapLayer_font = GetNode<TileMapLayer>("TileMapLayer_font");
        tileMapLayer_back = GetNode<TileMapLayer>("TileMapLayer_back");
        tileMapLayer_shadow = GetNode<TileMapLayer>("TileMapLayer_shadow");
        debugView = GetNode<DebugView>("DebugView");
        tileMapLayer_font.TileSet = Materials.CreateTileSet();
        tileMapLayer_back.TileSet = Materials.CreateTileSet();
        
    }


    public override void _Process(double delta)
    {
        if (chunk == null || GetParent() == null)
        {
            QueueFree();
        }
        
        if (chunk.update_tilemap)
        {
            debugView.time = time++;
            debugView.chunk = chunk;
            debugView.QueueRedraw();
            for (int z = 0; z < Chunk.SizeZ && z < 2; z++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    for (int y = 0; y < Chunk.Size; y++)
                    {
                        var pos = new Vector2I(x, y);
                        Blockdata block = chunk.GetBlock(x, y, z);
                        Blockdata block_back = chunk.GetBlock(x, y, 0);
                        Blockdata block_font = chunk.GetBlock(x, y, 1);

                        //前景图方块不可见，后面也不可见
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

                        int tile_size = block.GetTileSize();
                        Vector2I coord = new(0, 0);
                        if (block.BlockMeta.Tiletype == "tile")
                        {
                            coord = new(x % tile_size, y % tile_size);
                        }

                        if (block.BlockMeta.Tiletype == "atlas")
                        {
                            coord = new(block.STATE, 0);
                        }

                        if (block.BlockMeta.Tiletype == "terrain")
                        {
                            var tag = block.BlockMeta.GetTag("link");
                            var global = new Vector3I(chunk.X * Chunk.Size + x, chunk.Y * Chunk.Size + y, z);
                            coord = player.world.WorldService.GetTerrain(global, "link", tag);
                        }

                        if (z == 0 && !block_font.BlockMeta.CUBE)
                        {
                            if (bts != null && bts.scene)
                            {
                                if (tileMapLayer_back.GetCellSourceId(pos) != tile_id)
                                    tileMapLayer_back.SetCell(pos, tile_id, Vector2I.Zero, bts.id);
                            }

                            else if (tileMapLayer_back.GetCellSourceId(pos) != tile_id)
                                tileMapLayer_back.SetCell(new(x, y), tile_id, coord);
                        }
                        else
                        {
                            if (tileMapLayer_shadow.GetCellAtlasCoords(pos) != new Vector2I(block_font.Light, 0))
                                tileMapLayer_shadow.SetCell(new(x, y), 0, new Vector2I(block_font.Light, 0));

                            if (bts != null && bts.scene)
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