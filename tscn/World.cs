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
        private PackedScene chunkPackedScene = GD.Load<PackedScene>("res://tscn/TileMapLayerChunk.tscn");

        public enum WorldMode
        {
            Preview, //预览模式,仅生成世界,不保存,不加载
            Single, //单人模式,拥有全部内容
            MultiplayerClient, //联机客户端模式,不生成世界,不保存，不加载
            MultiplayerHost //联机服主机模式,拥有全部内容
        }

        /// <summary>
        /// 世界名
        /// </summary>
        public static string WorldName = "";

        /// <summary>
        /// 世界种子
        /// </summary>
        public static long Seed;

        /// <summary>
        /// 世界模式
        /// </summary>
        public static WorldMode worldMode = WorldMode.Single;

        /// <summary>
        /// 世界服务类
        /// </summary>
        public WorldServiceBase Service;

        /// <summary>
        /// 世界时刻总耗时 ms
        /// </summary>
        public double TimeConsumingμs = 0;

        /// <summary>
        /// 区块TileMap集合
        /// </summary>
        /// 
        //public List<TileMapLayerChunk> tileMapLayerChunks = new();
        public Dictionary<Vector2I, TileMapLayerChunk> tileMapLayerChunks = new();

        /// <summary>
        /// 用于显示的区块集合
        /// </summary>
        public Dictionary<Vector2I, Chunk> VisibleChunks = new();


        //Node
        [Export] public PlayerNode PlayerNode;
        [Export] public Timer timer;
        [Export] public TextureRect textureRect;
        [Export] public DirectionalLight2D DirectionalLight2D;
        [Export] public ColorRect colorRect;

        /// <summary>
        /// Rpc请求冷却
        /// </summary>
        public double RequeueFreeze = 0;


        Stopwatch _stopwatch = new Stopwatch();

        public override void _Ready()
        {
            _ = Materials.BlockMetas;

            timer.Timeout += ClientTick;

            if (worldMode == WorldMode.Single)
                Service = new SingleWorldService(this);

            if (worldMode == WorldMode.MultiplayerHost)
                Service = new HostWorldService(this);

            if (worldMode == WorldMode.MultiplayerClient)
                Service = new ClientWorldService(this);


            if (worldMode == WorldMode.Preview)
            {
                Seed = Random.Shared.NextInt64();
                Service = new PreviewWorldService(this);
            }

            //先初始化世界服务本身，再初始化世界服务对应的功能服务，以免出现循环引用
            Service.InitializeServices();
        }

        public override void _ExitTree()
        {
            Service.Save();
        }

        public override void _Process(double delta)
        {
            //更新天空背景颜色
            textureRect.Modulate = Service.GetSkyColor();
            //更新覆盖的光线明暗度变化
            colorRect.Color = Color.Color8(0, 0, 0, Service.GetLightChange());
            //更新请求冷却
            if (RequeueFreeze > 0) RequeueFreeze -= delta;
            else RequeueFreeze = 0;

            //重新请求玩家数据
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

        /// <summary>
        /// 客户端时刻
        /// </summary>
        public void ClientTick()
        {
            _stopwatch.Restart();

            UpdateTileMap();
            BlockInterFaceHandle();

            _stopwatch.Stop();
            Service.TickTimes++;

            TimeConsumingμs = _stopwatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// 当前区块坐标的区块是否有TileMap节点，通常用于判断实体所在的区域是否存在TileMap
        /// </summary>
        /// <param name="coord">区块坐标</param>
        /// <returns>是否存在TileMap节点</returns>
        public bool HasTileMap(Vector2I coord)
        {
            return tileMapLayerChunks.ContainsKey(coord);
        }

        /// <summary>
        /// 尝试添加新的TileMap
        /// </summary>
        /// <param name="chunk"></param>
        private void TryAddTileMap(Chunk chunk)
        {
            if (tileMapLayerChunks.ContainsKey(chunk.coord))
            {
                tileMapLayerChunks[chunk.coord].SetChunk(chunk);
                return;
            }

            TileMapLayerChunk tmly = chunkPackedScene.Instantiate<TileMapLayerChunk>();
            tmly.chunk = chunk;
            chunk.update_tilemap = true;
            tmly.GlobalPosition = chunk.coord * Chunk.Size * 16;
            tmly.Visible = true;
            tmly.PlayerNode = PlayerNode;
            tileMapLayerChunks.Add(chunk.coord, tmly);
            AddChild(tmly);
        }

        /// <summary>
        /// 更新TileMap
        /// </summary>
        public void UpdateTileMap()
        {
            if (PlayerNode.playerData == null) return;
            foreach (var tmly in tileMapLayerChunks.Values.ToArray())
            {
                if (!VisibleChunks.ContainsKey(tmly.chunk.coord))
                {
                    RemoveChild(tmly);
                    tileMapLayerChunks.Remove(tmly.chunk.coord);
                    tmly.QueueFree();
                }
            }

            foreach (var key in VisibleChunks.Keys)
            {
                TryAddTileMap(VisibleChunks[key]);
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

        //TODO 这个功能还不知道怎么移植到服务类中去，目前只能在客户端即时生效如果用网络的话，可能会出现客户端玩家在因为延迟在天上飞的情况，还待研究，不过就这样也行，服务端可以做一个简易的反作弊检测
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
                // if (CurrentBlock.BlockMeta.Collide && PlayerNode.playerData.Mode == 0)
                // {
                //     PlayerNode.Position += new Vector2(0, -17);
                // }
            }

            if (PlayerNode.playerData.Mode == 0)
            {
                PlayerNode.playerData.Fly.Value = false;
                PlayerNode.playerData.Resistance.Value = 1f;
            }
        }
    }
}