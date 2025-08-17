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
    DebugView debugView;
    [Export] private float perspectiveOffsetFactor = 0.1f;

    public override void _Ready()
    {
        tileMapLayer_font = GetNode<TileMapLayer>("TileMapLayer_font");
        tileMapLayer_back = GetNode<TileMapLayer>("TileMapLayer_back");
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
            debugView.chunk = chunk;
            debugView.QueueRedraw();
            for (int z = 0; z < Chunk.SizeZ && z < 2; z++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    for (int y = 0; y < Chunk.Size; y++)
                    {
                        Blockdata block = chunk[x, y, z];
                        Blockdata block_back = chunk[x, y, 0];
                        Blockdata block_font = chunk[x, y, 1];

                        int tile_id = block.GetTileId();
                        Vector2I coord = new(0, 0);
                        if (block.BlockMeta.Tiletype == "tile")
                        {
                            coord = new(x % 2, y % 2);
                        }
                        else if (block.BlockMeta.Tiletype == "atlas")
                        {
                            coord = new(block.STATE, 0);
                        }

                        if (z == 0 && !block_font.BlockMeta.CUBE)
                            tileMapLayer_back.SetCell(new(x, y), tile_id, coord);
                        else
                            tileMapLayer_font.SetCell(new(x, y), tile_id, coord);
                    }
                }
            }

            chunk.update_tilemap = false;
        }
    }
}