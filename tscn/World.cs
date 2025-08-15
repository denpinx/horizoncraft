using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Godot;
using Godot.Collections;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.WorldControl;
using Microsoft.Data.Sqlite;
using Array = Godot.Collections.Array;
using Timer = Godot.Timer;

namespace horizoncraft.script
{
    public partial class World : Node2D
    {
        //事件
        public Action CilentTicked;
        public Action<Chunk> TileMapRemove;
        public Action<Chunk> TileMapAdd;
        //
        public static string world_name = "world";
        public long tick_use_time = 0;
        public SqliteConnection connection;
        public ChunkManageSql chunkManage;
        public PackedScene PSTilemapLayerChunk;
        public List<TileMapLayerChunk> tileMapLayerChunks = new();
        public System.Collections.Generic.Dictionary<Vector2I, Chunk> VisibleChunks = new();
        //Node
        public Player player;
        public Timer timer;
        public SubViewport subViewport;
        public TextureRect textureRect;
        public bool HasTileMap(Vector2I coord)
        {
            for (int i = 0; i < tileMapLayerChunks.Count; i++)
            {
                if (tileMapLayerChunks[i].chunk.coord == coord)
                {
                    return true;
                }
            }
            return false;
        }
        public void RemoveTileMap(Vector2I coord)
        {
            for (int i = 0; i < tileMapLayerChunks.Count; i++)
            {
                if (tileMapLayerChunks[i].chunk.coord == coord)
                {
                    TileMapRemove?.Invoke(tileMapLayerChunks[i].chunk);

                    tileMapLayerChunks[i].QueueFree();
                    tileMapLayerChunks.RemoveAt(i);

                    break;
                }
            }
        }

        public void AddTileMap(Chunk chunk)
        {
            for (int i = 0; i < tileMapLayerChunks.Count; i++)
            {
                if (tileMapLayerChunks[i].chunk.coord == chunk.coord)
                {
                    chunk.update = true;
                    tileMapLayerChunks[i].chunk = chunk;
                    return;
                }
            }
            TileMapLayerChunk tmly = PSTilemapLayerChunk.Instantiate<TileMapLayerChunk>();
            tmly.chunk = chunk;
            tmly.GlobalPosition = chunk.coord * Chunk.Size * 16;
            tmly.Visible = true;
            tmly.player = player;
            tileMapLayerChunks.Add(tmly);
            AddChild(tmly);
            TileMapAdd?.Invoke(chunk);
        }

        public override void _Ready()
        {
            _ = Materials.blockmetas;

            GD.Print("_Ready()");
            PSTilemapLayerChunk = GD.Load<PackedScene>("res://tscn/TileMapLayerChunk.tscn");
            player = GetNode<Player>("Player");
            timer = GetNode<Timer>("Timer_Tick");
            subViewport = GetNode<SubViewport>("CanvasLayer/SubViewport");
            textureRect = GetNode<TextureRect>("CanvasLayer/TextureRect");

            textureRect.Texture = subViewport.GetTexture();

            timer.Timeout += CilentTick;
            player.world = this;

            chunkManage = new ChunkManageSql(this);
            chunkManage.OnPlayerMoveChunk();
        }

        public override void _ExitTree()
        {
            chunkManage.Save();
        }

        public override void _Process(double delta) { }

        public void CilentTick()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < tileMapLayerChunks.Count; i++)
            {
                TileMapLayerChunk tmly = tileMapLayerChunks[i];
                if (!VisibleChunks.ContainsKey(tmly.chunk.coord))
                {
                    RemoveChild(tmly);
                    tileMapLayerChunks.RemoveAt(i);
                    tmly.QueueFree();
                }
            }
            foreach (var key in VisibleChunks.Keys)
            {
                AddTileMap(VisibleChunks[key]);
            }
            CilentTicked?.Invoke();


            player.Save();
            sw.Stop();
            tick_use_time = sw.ElapsedMilliseconds;
        }

        public static Vector2I MathFloor(Vector3I V3I, int chunkSize)
        {
            return new Vector2I(
                (int)Mathf.Floor((float)V3I.X / chunkSize),
                (int)Mathf.Floor((float)V3I.Y / chunkSize)
            );
        }

        public static Vector2I MathFloor(Vector2I V2I, int chunkSize)
        {
            return new Vector2I(
                (int)Mathf.Floor((float)V2I.X / chunkSize),
                (int)Mathf.Floor((float)V2I.Y / chunkSize)
            );
        }

        public static int MathFloor(int integer, int chunkSize)
        {
            return (int)Mathf.Floor((float)integer / chunkSize);
        }

        public static Vector2I Remainder(Vector3I V3I, int chunkSize)
        {
            return new Vector2I(
                (V3I.X % chunkSize + chunkSize) % chunkSize,
                (V3I.Y % chunkSize + chunkSize) % chunkSize
            );
        }
        public static int Remainder(int integer, int chunkSize)
        {
            return (integer % chunkSize + chunkSize) % chunkSize;
        }
    }
}
