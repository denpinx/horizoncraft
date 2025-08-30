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
        public PackedScene PlayerSnapshot_ps;

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
        public System.Collections.Generic.Dictionary<string, PlayerSnapshot> PlayerNodes = new();
        public System.Collections.Generic.Dictionary<Vector2I, Chunk> VisibleChunks = new();

        //Node
        public Player player;
        public Timer timer;
        public TextureRect textureRect;
        public DirectionalLight2D DirectionalLight2D;
        public ColorRect colorRect;
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
                    chunk.update_tilemap = true;
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
            _ = Materials.BlockMetas;
            PlayerSnapshot_ps = GD.Load<PackedScene>("res://tscn/PlayerSnapshot.tscn");
            PSTilemapLayerChunk = GD.Load<PackedScene>("res://tscn/TileMapLayerChunk.tscn");
            player = GetNode<Player>("Player");
            timer = GetNode<Timer>("Timer_Tick");
            textureRect = GetNode<TextureRect>("CanvasLayer_Back/TextureRect_Sky");
            colorRect = GetNode<ColorRect>("CanvasLayer/ColorRect_Top");
            DirectionalLight2D = GetNode<DirectionalLight2D>("DirectionalLight2D");
            timer.Timeout += ClientTick;
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
                player.playerData = new PlayerData() { Name = Player.Profile.Name };
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
            if (WorldService != null && colorRect != null)
            {
                float t = WorldService.GetTimeProgress();
                colorRect.Color = Color.Color8(0, 0, 0, GetLightChange(t));
                textureRect.Modulate = GetSkyChange(t);
                DirectionalLight2D.Energy = 1 - GetLightChange(t) / 255f;
                DirectionalLight2D.RotationDegrees = (1 - t) * 360f + 180f;
            }

            if (RequeueFreeze > 0) RequeueFreeze -= delta;
            if (player.playerData == null)
            {
                if (WorldService is WorldHostService whs)
                {
                    PlayerData pd;
                    if (whs.GetPlayer(Player.Profile.Name, out pd))
                    {
                        player.playerData = pd;
                        player.Position = pd.Position_v2;
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
                        RpcId(1, "GetPlayer", Player.Profile.Name, Multiplayer.GetUniqueId());
                        RequeueFreeze = 2;
                    }
                }
                else if (WorldService is IWorldService iws &&
                         iws.GetPlayer(Player.Profile.Name, out player.playerData) &&
                         player.playerData != null)
                {
                    player.playerData.player = player;
                    player.Position = new(player.playerData.Position.X, player.playerData.Position.Y);
                    if (worldMode == WorldMode.Preview)
                    {
                        player.Visible = false;
                        player.Visible = false;
                    }

                    iws.UpdateLoadChunkCoords();
                }
            }
        }

        public void ClientTick()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (var tmly in tileMapLayerChunks.ToArray())
            {
                if (!VisibleChunks.ContainsKey(tmly.chunk.coord))
                {
                    RemoveChild(tmly);
                    tileMapLayerChunks.Remove(tmly);
                    tmly.QueueFree();
                }
            }

            foreach (var key in VisibleChunks.Keys)
            {
                AddTileMap(VisibleChunks[key]);
            }

            CilentTicked?.Invoke();
            SyncPlayerSnapshot();
            BlockInterFaceHandle();
            sw.Stop();
            tick_use_time = sw.ElapsedMilliseconds;
        }


        public void SyncPlayerSnapshot()
        {
            if (WorldService is not WorldPreviewService)
                foreach (var Kvp in WorldService.Players)
                {
                    //添加玩家节点
                    if (!PlayerNodes.ContainsKey(Kvp.Key))
                    {
                        if (Kvp.Key != Player.Profile.Name)
                        {
                            PlayerSnapshot player = PlayerSnapshot_ps.Instantiate<PlayerSnapshot>();
                            PlayerNodes.Add(Kvp.Key, player);
                            AddChild(player);
                        }
                    }
                    else
                    {
                        //更新玩家节点
                        PlayerNodes[Kvp.Key].SetData(Kvp.Value);
                        if (Kvp.Key != Player.Profile.Name)
                        {
                            PlayerNodes[Kvp.Key].Position = Kvp.Value.Position_v2;
                            //Tween tween = GetTree().CreateTween();
                            //tween.TweenProperty(PlayerNodes[Kvp.Key], "position", Kvp.Value.Position_v2, 0.05f);
                        }
                    }
                }

            //删除不可见玩家对象
            if (WorldService is WorldClientService && player.playerData != null)
            {
                foreach (var Kvp in WorldService.Players)
                {
                    //删除玩家节点
                    if (Kvp.Key != Player.Profile.Name)
                    {
                        PlayerData playerData = Kvp.Value;
                        if (
                            Math.Abs(playerData.ChunkCoord.X - player.playerData.ChunkCoord.X) >
                            WorldService.LoadHorizon ||
                            Math.Abs(playerData.ChunkCoord.Y - player.playerData.ChunkCoord.Y) >
                            WorldService.LoadHorizon
                        )
                        {
                            WorldService.Players.TryRemove(Kvp.Key, out _);
                        }
                    }
                }
            }

            if (WorldService is WorldClientService or WorldHostService)
                foreach (var kvp in PlayerNodes)
                {
                    if (!WorldService.Players.ContainsKey(kvp.Key))
                    {
                        var p = kvp.Value;
                        RemoveChild(p);
                        PlayerNodes.Remove(kvp.Key);
                        p.QueueFree();
                    }
                }
        }


        public void BlockInterFaceHandle()
        {
            if (player?.playerData == null) return;

            Blockdata CurrentBlock =
                WorldService?.GetBlock(new Vector3I(player.playerData.Coord.X, player.playerData.Coord.Y, 1));
            if (CurrentBlock != null)
            {
                string tag = CurrentBlock.GetTag("enter_action");
                if (tag != null)
                {
                    if (tag == "no-fall")
                    {
                        player.playerData.Fly.Value = true;
                        player.playerData.Resistance.Value = 0.5f;
                        return;
                    }
                }

                //防止卡墙里
                if (CurrentBlock.BlockMeta.Collide && player.playerData.Mode == 0)
                {
                    player.Position += new Vector2(0, -17);
                }
            }

            if (player.playerData.Mode == 0)
            {
                player.playerData.Fly.Value = false;
                player.playerData.Resistance.Value = 1f;
            }
        }


        public byte GetLightChange(float t)
        {
            float hour = t * 24f;
            if (hour < 5f || hour >= 20f)
                return 200;
            else if (hour >= 5f && hour < 8f)
                return (byte)(200 * (1f - (hour - 5f) / 3f));
            else if (hour >= 8f && hour < 17f)
                return 0;
            else
                return (byte)(200 * ((hour - 17f) / 3f));
        }

        public Color GetSkyChange(float t)
        {
            float hour = t * 24f;

            if (hour >= 0 && hour < 6) // 00:00 - 06:00
            {
                float p = hour / 6f;
                byte r = (byte)Math.Clamp(15 + p * (25 - 15), 0, 255);
                byte g = (byte)Math.Clamp(25 + p * (55 - 25), 0, 255);
                byte b = (byte)Math.Clamp(55 + p * (135 - 55), 0, 255);
                return Color.Color8(r, g, b);
            }

            if (hour >= 6 && hour < 12) // 06:00 - 12:00
            {
                float p = (hour - 6f) / 6f;
                byte r = (byte)Math.Clamp(25 + p * (135 - 25), 0, 255);
                byte g = (byte)Math.Clamp(55 + p * (175 - 55), 0, 255);
                byte b = (byte)Math.Clamp(135 + p * (255 - 135), 0, 255);
                return Color.Color8(r, g, b);
            }

            if (hour >= 12 && hour < 18) // 12:00 - 18:00
            {
                float p = (hour - 12f) / 6f;
                byte r = (byte)Math.Clamp(135 + p * (255 - 135), 0, 255);
                byte g = (byte)Math.Clamp(175 + p * (135 - 175), 0, 255);
                byte b = (byte)Math.Clamp(255 + p * (0 - 255), 0, 255);
                return Color.Color8(r, g, b);
            }

            if (hour >= 18 && hour < 24) // 18:00 - 24:00
            {
                float p = (hour - 18f) / 6f;
                byte r = (byte)Math.Clamp(255 + p * (15 - 255), 0, 255);
                byte g = (byte)Math.Clamp(135 + p * (25 - 135), 0, 255);
                byte b = (byte)Math.Clamp(0 + p * (55 - 0), 0, 255);
                return Color.Color8(r, g, b);
            }

            return Color.Color8(255, 255, 255);
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