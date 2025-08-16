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
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;
using Microsoft.Data.Sqlite;
using Array = Godot.Collections.Array;
using Timer = Godot.Timer;

namespace horizoncraft.script
{
    public partial class World : Node2D
    {
        //
        public enum WorldMode
        {
            Preview, //预览模式,仅生成世界,不保存,不加载
            Single, //单人模式,拥有全部内容
            MultiplayerClient, //联机客户端模式,不生成世界,不保存，不加载
            MultiplayerHost //联机服主机模式,拥有全部内容
        }

        public static WorldMode worldMode = WorldMode.Single;

        //
        public PackedScene Player_ps;

        //事件
        public Action CilentTicked;
        public Action<Chunk> TileMapRemove;

        public Action<Chunk> TileMapAdd;

        //
        public static string world_name = "world";
        public long tick_use_time = 0;
        public SqliteConnection connection;
        public WorldBase WorldService;
        public PackedScene PSTilemapLayerChunk;
        public List<TileMapLayerChunk> tileMapLayerChunks = new();
        public System.Collections.Generic.Dictionary<string, Player> PlayerNodes = new();
        public System.Collections.Generic.Dictionary<Vector2I, Chunk> VisibleChunks = new();

        //Node
        public Player player;
        public Timer timer;
        public SubViewport subViewport;
        public TextureRect textureRect;

        public double RequeueFreeze = 0;

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
            Player_ps = GD.Load<PackedScene>("res://tscn/Player.tscn");
            PSTilemapLayerChunk = GD.Load<PackedScene>("res://tscn/TileMapLayerChunk.tscn");
            player = GetNode<Player>("Player");
            timer = GetNode<Timer>("Timer_Tick");
            subViewport = GetNode<SubViewport>("CanvasLayer/SubViewport");
            textureRect = GetNode<TextureRect>("CanvasLayer/TextureRect");

            textureRect.Texture = subViewport.GetTexture();

            timer.Timeout += CilentTick;
            player.world = this;

            if (worldMode == WorldMode.Single)
            {
                WorldSingleService wsw = new WorldSingleService();
                WorldService = wsw;
                wsw.world = this;
                wsw.Init();
                wsw.UpdateLoadChunkCoords();

                player.Inputable = true;
                player.MoreInfo = true;
            }

            if (worldMode == WorldMode.MultiplayerHost)
            {
                WorldHostService whs = new WorldHostService();
                WorldService = whs;
                whs.world = this;
                whs.Init();
                whs.UpdateLoadChunkCoords();
            }

            if (worldMode == WorldMode.MultiplayerClient)
            {
                WorldClientService wcs = new WorldClientService();
                WorldService = wcs;
                wcs.world = this;
                wcs.Init();
                wcs.UpdateLoadChunkCoords();
            }

            if (worldMode == WorldMode.Preview)
            {
                WorldPreviewService wps = new WorldPreviewService();
                WorldService = wps;
                wps.world = this;
                wps.Init();
                wps.UpdateLoadChunkCoords();
                player.playerData = new PlayerData() { Name = "Player" };

                player.Inputable = false;
                player.Visible = false;
                player.MoreInfo = false;
            }
        }

        public override void _ExitTree()
        {
            if (WorldService is IWorldService IWS)
                IWS.Save();
        }

        public override void _Process(double delta)
        {
            if (RequeueFreeze > 0) RequeueFreeze -= delta;
            if (player.playerData == null)
            {
                if (WorldService is WorldHostService whs)
                {
                    PlayerData pd;
                    if (whs.GetPlayer(Player.LocalName, out pd))
                    {
                        player.playerData = pd;
                        player.Position = pd.Position;
                        GD.Print("[服务端]初始化玩家成功");
                    }
                }
                else if (WorldService is WorldPreviewService pws)
                {
                    player.playerData = new PlayerData() { Name = "Player" };
                }
                else if (WorldService is WorldClientService wcs && WorldService.Connect)
                {
                    if (RequeueFreeze <= 0)
                    {
                        RpcId(1, "GetPlayer", Player.LocalName, Multiplayer.GetUniqueId());
                        RequeueFreeze = 2;
                    }
                }
                else if (WorldService is IWorldService iws && iws.GetPlayer(Player.LocalName, out player.playerData) &&
                         player.playerData != null)
                {
                    player.playerData.player = player;
                    player.Position = player.playerData.Position;
                    if (worldMode == WorldMode.Preview)
                    {
                        player.Visible = false;
                        player.Visible = false;
                    }

                    iws.UpdateLoadChunkCoords();
                }
            }

            if (WorldService is WorldBase worldBase && WorldService is not WorldPreviewService)
                foreach (var Kvp in worldBase.Players)
                {
                    //添加玩家节点
                    if (!PlayerNodes.ContainsKey(Kvp.Key))
                    {
                        if (Kvp.Key != Player.LocalName)
                        {
                            Player player = Player_ps.Instantiate<Player>();
                            player.playerData = Kvp.Value;
                            player.world = this;
                            player.Inputable = false;
                            PlayerNodes.Add(Kvp.Key, player);
                            AddChild(player);
                        }
                    }
                    else
                    {
                        //更新玩家节点
                        PlayerNodes[Kvp.Key].playerData = Kvp.Value;
                        if (Kvp.Key != Player.LocalName)
                        {
                            //PlayerNodes[Kvp.Key].Position = Kvp.Value.Position;
                            Tween tween = GetTree().CreateTween();
                            tween.TweenProperty(PlayerNodes[Kvp.Key], "position", Kvp.Value.Position, 0.05f);
                        }
                    }
                }
        }

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

            //删除不可见玩家对象
            if ((WorldService is WorldHostService || WorldService is WorldSingleService) && player.playerData != null &&
                WorldService is WorldBase worldBase)
            {
                foreach (var Kvp in worldBase.Players)
                {
                    //删除玩家节点
                    if (Kvp.Key != Player.LocalName)
                    {
                        PlayerData playerData = Kvp.Value;
                        if (
                            Math.Abs(player.playerData.ChunkCoord.X - player.playerData.ChunkCoord.X) >
                            WorldService.LoadHorizon ||
                            Math.Abs(player.playerData.ChunkCoord.Y - player.playerData.ChunkCoord.Y) >
                            WorldService.LoadHorizon
                        )
                        {
                            worldBase.Players.TryRemove(Kvp.Key, out _);
                        }
                    }
                }

                foreach (var kvp in PlayerNodes)
                {
                    if (!worldBase.Players.ContainsKey(kvp.Key))
                    {
                        var p = kvp.Value;
                        RemoveChild(p);
                        PlayerNodes.Remove(kvp.Key);
                        p.QueueFree();
                    }
                }
            }

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