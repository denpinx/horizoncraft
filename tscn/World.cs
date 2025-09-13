using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using horizoncraft.script.Config;
using HorizonCraft.script.Services.world;
using horizoncraft.script.WorldControl;
using Microsoft.Data.Sqlite;
using Array = Godot.Collections.Array;
using Timer = Godot.Timer;

namespace horizoncraft.script
{
    //第一次重构:策略模式，但是功能聚合
    //第二次重构:功能组合，代理模式
    //第三次重构:世界服务策略模式，通过功能服务组合，功能服务也是不同策略模式的实现。
    public partial class World : Node2D
    {
        public enum WorldMode
        {
            Preview, //预览模式,仅生成世界,不保存,不加载
            Single, //单人模式,拥有全部内容
            MultiplayerClient, //联机客户端模式,不生成世界,不保存，不加载
            MultiplayerHost //联机服主机模式,拥有全部内容
        }

        public static string WorldName = "";
        public static long Seed;

        public static WorldMode worldMode = WorldMode.Single;
        public WorldServiceBase Service;
        public long tick_use_time = 0;
        public List<TileMapLayerChunk> tileMapLayerChunks = new();
        public Dictionary<Vector2I, Chunk> VisibleChunks = new();
        public Action CilentTicked;


        private PackedScene PSTilemapLayerChunk;

        //Node
        public PlayerNode PlayerNode;
        public Timer timer;
        public TextureRect textureRect;
        public DirectionalLight2D DirectionalLight2D;
        public ColorRect colorRect;
        public double RequeueFreeze = 0;


        public override void _Ready()
        {
            _ = Materials.BlockMetas;
            PSTilemapLayerChunk =
                GD.Load<PackedScene>("res://tscn/TileMapLayerChunk.tscn");
            textureRect =
                GetNode<TextureRect>("CanvasLayer_Back/TextureRect_Sky");
            DirectionalLight2D =
                GetNode<DirectionalLight2D>("DirectionalLight2D");
            colorRect =
                GetNode<ColorRect>("CanvasLayer/ColorRect_Top");
            PlayerNode =
                GetNode<PlayerNode>("Player");
            timer =
                GetNode<Timer>("Timer_Tick");

            timer.Timeout += ClientTick;
            PlayerNode.world = this;

            if (worldMode == WorldMode.Single)
                Service = new SingleWorldService(this);

            if (worldMode == WorldMode.MultiplayerHost)
                Service = new HostWorldService(this);

            if (worldMode == WorldMode.MultiplayerClient)
                Service = new ClientWorldService(this);


            if (worldMode == WorldMode.Preview)
            {
                Seed = System.Random.Shared.NextInt64();
                Service = new PreviewWorldService(this);
            }

            Service.InitializeServices();
        }

        public override void _ExitTree()
        {
            Service.ChunkService.SaveAll();
            Service.PlayerService.SaveAll();
        }

        public override void _Process(double delta)
        {
            textureRect.Modulate = GetSkyChange();
            if (RequeueFreeze > 0) RequeueFreeze -= delta;
            else RequeueFreeze = 0;


            if (PlayerNode.playerData == null && RequeueFreeze == 0)
            {
                if (Service.PlayerService.GetPlayerOrLoad(PlayerNode.Profile.Name, out var data))
                {
                    PlayerNode.playerData = data;
                    PlayerNode.Position = data.Position_v2;
                    GD.Print($"玩家获取成功{data.Name}{data.Coord}");
                }
                else
                {
                    GD.Print("玩家获取失败");
                    RequeueFreeze = 0.5;
                }
            }
        }

        public void ClientTick()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            UpdateTileMap();
            CilentTicked?.Invoke();
            BlockInterFaceHandle();
            sw.Stop();

            Service.TickTimes++;

            tick_use_time = sw.ElapsedMilliseconds;
        }


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

        private void AddTileMap(Chunk chunk)
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
            tmly.PlayerNode = PlayerNode;
            tileMapLayerChunks.Add(tmly);
            AddChild(tmly);
        }

        public void UpdateTileMap()
        {
            if (PlayerNode.playerData == null) return;
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

            HashSet<Vector2I> poss = new HashSet<Vector2I>();
            Service.ChunkService.GetLoadRangeChunks(PlayerNode.playerData.ChunkCoord, poss);
            //更新
            foreach (var pos in poss)
            {
                if (Service.ChunkService.Chunks.TryGetValue(pos, out var chunk))
                {
                    VisibleChunks[pos] = chunk;
                }
            }

            //去重卸载
            foreach (Vector2I coord in VisibleChunks.Keys.ToArray())
            {
                if (!poss.Contains(coord))
                    VisibleChunks.Remove(coord, out _);
            }
        }

        //TODO 这个功能还不知道怎么移植到服务类中去，目前只能在客户端即时生效如果用网络的话，可能会出现客户端玩家在因为延迟在天上飞的情况，还待研究
        public virtual void BlockInterFaceHandle()
        {
            if (PlayerNode?.playerData == null) return;

            BlockData CurrentBlock =
                Service.ChunkService.GetBlock(new Vector3I(PlayerNode.playerData.Coord.X, PlayerNode.playerData.Coord.Y,
                    1));
            if (CurrentBlock != null)
            {
                string tag = CurrentBlock.GetTag("enter_action");
                if (tag != null)
                {
                    if (tag == "no-fall")
                    {
                        PlayerNode.playerData.Fly.Value = true;
                        PlayerNode.playerData.Resistance.Value = 0.5f;
                        return;
                    }
                }

                //防止卡墙里
                if (CurrentBlock.BlockMeta.Collide && PlayerNode.playerData.Mode == 0)
                {
                    PlayerNode.Position += new Vector2(0, -17);
                }
            }

            if (PlayerNode.playerData.Mode == 0)
            {
                PlayerNode.playerData.Fly.Value = false;
                PlayerNode.playerData.Resistance.Value = 1f;
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

        public Color GetSkyChange()
        {
            float hour = Service.GetTimeHour();

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


        public static Vector2I MathFloor(Vector2I V2I, int chunkSize)
        {
            return new Vector2I(
                (int)Mathf.Floor((float)V2I.X / chunkSize),
                (int)Mathf.Floor((float)V2I.Y / chunkSize)
            );
        }

        public static Vector2I MathFloor(Vector3I V2I, int chunkSize)
        {
            return new Vector2I(
                (int)Mathf.Floor((float)V2I.X / chunkSize),
                (int)Mathf.Floor((float)V2I.Y / chunkSize)
            );
        }

        public static Vector2I Remainder(Vector3I V3I, int chunkSize)
        {
            return new Vector2I(
                (V3I.X % chunkSize + chunkSize) % chunkSize,
                (V3I.Y % chunkSize + chunkSize) % chunkSize
            );
        }
    }
}