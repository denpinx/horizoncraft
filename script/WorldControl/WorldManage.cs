using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Features;
using HorizonCraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.work;
using MemoryPack;
using Microsoft.Data.Sqlite;
using FileAccess = Godot.FileAccess;

namespace horizoncraft.script.WorldControl
{
    public partial class WorldManage : IBaseManage
    {
        public enum WorldMode
        {
            Preview, //预览模式,仅生成世界,不保存,不加载
            Single, //单人模式,拥有全部内容
            MultiplayerClient, //联机客户端模式,不生成世界,不保存，不加载
            MultiplayerHost //联机服主机模式,拥有全部内容
        }

        public event Action<WorldManage, Chunk> OnChunkLoaded;
        public event Action<WorldManage, Chunk> OnChunkUnLoading;


        public static WorldMode worldMode = WorldMode.Single;

        public bool Connect = false;
        public long time;
        public long tick_use_time;
        public int LoadHorizon = 3;

        public int TileMapHorizon = 2;

        //已加载区块
        public ConcurrentDictionary<Vector2I, Chunk> LoadedChunks = new();

        //待卸载区块
        public ConcurrentDictionary<Vector2I, Chunk> UnloadingQuee = new();
        public ConcurrentQueue<string> LoadingPlayers = new();

        // 待加载区块以及加载后处理工作
        public ConcurrentDictionary<Vector2I, WorkBase> LoadingQuee = new();
        public ConcurrentDictionary<string, PlayerData> playerdatas = new();
        public bool Lock = false;
        public World world;
        SqliteConnection sqliteConnection;

        public WorldManage(World world)
        {
            WorldManage.worldMode = worldMode;
            this.world = world;
            world.timer.Timeout += PreTick;
            world.player.OnMoveToChunk += OnPlayerMoveChunk;
            if (worldMode == WorldMode.Single || worldMode == WorldMode.MultiplayerHost)
            {
                InitSqlite();
                if (worldMode == WorldMode.MultiplayerHost)
                {
                    GD.Print($"[{time}] 开启服务器");
                    //开启rpc服务器
                    world.Multiplayer.PeerDisconnected += (id) =>
                    {
                        foreach (var ps in playerdatas)
                        {
                            if (ps.Value.PeerId == id)
                            {
                                if (playerdatas.TryRemove(ps.Key, out _))
                                {
                                    GD.Print($"[{time}] 玩家断开连接");
                                }
                            }
                        }
                    };
                    world.Multiplayer.PeerConnected += (id) => { GD.Print($"[{time}] 玩家连接"); };
                    var peer = new ENetMultiplayerPeer();
                    peer.CreateServer(9999, 16);
                    world.Multiplayer.MultiplayerPeer = peer;
                }
                else
                {
                    GD.Print($"[{time}] 单人模式");
                }
            }
            else if (worldMode == WorldMode.MultiplayerClient)
            {
                GD.Print($"[{time}] 连接服务器");
                //连接服务器
                world.Multiplayer.PeerConnected += id =>
                {
                    GD.Print($"客户端连接成功{id}");
                    world.RpcId(1, "ConnectDone", Player.LocalName, world.Multiplayer.GetUniqueId());
                    Connect = true;
                };
                bool isfalse = false;
                world.Multiplayer.ConnectionFailed += () =>
                {
                    isfalse = true;
                    GD.PrintErr($"客户端连接失败！");
                    worldMode = WorldMode.Preview;
                    Connect = false;
                };
                var peer = new ENetMultiplayerPeer();
                peer.CreateClient("localhost", 9999);
                world.Multiplayer.MultiplayerPeer = peer;
                if (isfalse) return;
            }
            else
            {
                GD.Print($"[{time}] 世界预览模式");
            }
        }

        public void OnPlayerMoveChunk()
        {
            if (Connect) world.RpcId(1, "OnMoveAChunk");
            LoadingQuee.Clear();
            //遍历所有玩家,计算加载区块
            foreach (var player in playerdatas)
            {
                SavePlayer(player.Value);
                Vector2I CenterCoord = player.Value.ChunkCoord;
                for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
                {
                    for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
                    {
                        Vector2I coord = new Vector2I(X, Y);
                        LoadingQuee[coord] = new WorkBase();
                    }
                }
            }

            //删除重叠区块
            foreach (Vector2I coord in LoadedChunks.Keys)
            {
                if (!LoadingQuee.ContainsKey(coord))
                {
                    Chunk chunk = LoadedChunks[coord];
                    UnloadingQuee[coord] = chunk;
                    LoadedChunks.TryRemove(coord, out _);
                }
                else
                {
                    LoadingQuee.TryRemove(coord, out _);
                }
            }
        }

        public void PreTick()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //服务端更新玩家数据
            if (worldMode == WorldMode.MultiplayerHost)
            {
                foreach (var Fs in playerdatas)
                {
                    foreach (var Ts in playerdatas)
                    {
                        if (Fs.Key != Ts.Key)
                        {
                            PlayerData pd1 = Fs.Value;
                            PlayerData pd2 = Ts.Value;

                            if (
                                Math.Abs((pd1.ChunkCoord.X - pd2.ChunkCoord.X)) <= TileMapHorizon &&
                                Math.Abs((pd1.ChunkCoord.Y - pd2.ChunkCoord.Y)) <= TileMapHorizon
                            )
                            {
                                if (pd1.Name != Player.LocalName)
                                    world.RpcId(pd1.PeerId, "UpdataPosition", pd2.Name, pd2.Position.X, pd2.Position.Y);
                            }
                        }
                    }
                }

                foreach (var Ts in playerdatas)
                {
                    if (Player.LocalName != Ts.Key)
                    {
                        PlayerData pd2 = Ts.Value;
                        if (
                            Math.Abs(world.player.playerData.ChunkCoord.X - pd2.ChunkCoord.X) <= TileMapHorizon &&
                            Math.Abs(world.player.playerData.ChunkCoord.Y - pd2.ChunkCoord.Y) <= TileMapHorizon
                        )
                        {
                            world.RpcId(pd2.PeerId, "UpdataPosition", world.player.playerData.Name,
                                world.player.Position.X, world.player.Position.Y);
                        }
                    }
                }

                //更新区块数据
                foreach (var chunkset in LoadedChunks)
                {
                    if (chunkset.Value.update)
                        foreach (var playerset in playerdatas)
                        {
                            Chunk chunk = chunkset.Value;
                            PlayerData pd1 = playerset.Value;
                            if (
                                Math.Abs((chunk.X - pd1.ChunkCoord.X)) <= TileMapHorizon &&
                                Math.Abs((chunk.Y - pd1.ChunkCoord.Y)) <= TileMapHorizon
                            )
                            {
                                if (pd1.Name != Player.LocalName)
                                {
                                    Error error = world.RpcId(pd1.PeerId, "ReciveChunk", ChunkToByte(chunk));
                                    if (error != Error.Ok)
                                    {
                                        GD.PrintErr(pd1.Name, pd1.PeerId);
                                    }
                                }
                            }
                        }
                }
            }
            //客户端上传自身数据
            else if (worldMode == WorldMode.MultiplayerClient && Connect && world.player.playerData != null)
            {
                world.RpcId(1, "UpdataPosition", world.player.playerData.Name, world.player.Position.X,
                    world.player.Position.Y);
            }


            if (worldMode == WorldMode.Single || worldMode == WorldMode.MultiplayerHost ||
                worldMode == WorldMode.Preview)
            {
                if (LoadingPlayers.Count > 0)
                {
                    new Task(() =>
                    {
                        for (int i = LoadingPlayers.Count - 1; i >= 0; i--)
                        {
                            string name;
                            if (LoadingPlayers.TryDequeue(out name))
                            {
                                if (CheckPlayerExists(name))
                                {
                                    var bytes = GetPlayerByteData(name);
                                    PlayerData player = MemoryPackSerializer.Deserialize<PlayerData>(bytes);
                                    player.Name = name;
                                    playerdatas[player.Name] = player;
                                    GD.Print($"[{time}] 加载玩家数据:({name})");
                                }
                                else
                                {
                                    PlayerData player = new PlayerData()
                                    {
                                        Name = name
                                    };
                                    if (playerdatas.TryAdd(name, player))
                                    {
                                        GD.Print($"[{time}] 新建玩家数据:({name})");
                                    }
                                    else
                                    {
                                        if (LoadingPlayers.Contains(name))
                                            LoadingPlayers.Enqueue(name);
                                    }
                                }
                            }
                        }
                    }).Start();
                }
            }


            time++;
            if (world.player.playerData != null)
            {
                Vector2I CenterCoord = world.player.playerData.ChunkCoord;
                for (int X = CenterCoord.X - TileMapHorizon; X <= CenterCoord.X + TileMapHorizon; X++)
                {
                    for (
                        int Y = CenterCoord.Y - TileMapHorizon;
                        Y <= CenterCoord.Y + TileMapHorizon;
                        Y++
                    )
                    {
                        Vector2I coord = new Vector2I(X, Y);
                        if (LoadedChunks.ContainsKey(coord))
                        {
                            world.VisibleChunks[coord] = LoadedChunks[coord];
                        }
                    }
                }
            }


            var keysToRemove = new List<Vector2I>();
            foreach (Vector2I coord in world.VisibleChunks.Keys)
            {
                Chunk chunk = world.VisibleChunks[coord];
                Vector2I Horizon = chunk.coord - world.player.playerData.ChunkCoord;
                Horizon.X = Mathf.Abs(Horizon.X);
                Horizon.Y = Mathf.Abs(Horizon.Y);

                // 合并判断条件
                if (
                    Horizon.X > TileMapHorizon
                    || Horizon.Y > TileMapHorizon
                    || !LoadedChunks.ContainsKey(coord)
                )
                {
                    keysToRemove.Add(coord);
                }
            }

            foreach (var key in keysToRemove)
            {
                world.VisibleChunks.Remove(key, out _);
            }

            //保存区块
            if (WorldManage.worldMode == WorldMode.Single ||
                WorldManage.worldMode == WorldMode.MultiplayerHost)
            {
                foreach (Vector2I coord in UnloadingQuee.Keys)
                {
                    int max = UnloadingQuee.Count;
                    int count = 0;
                    Chunk chunk = UnloadingQuee[coord];
                    Chunk outchunk;
                    if (!Lock && UnloadingQuee.TryRemove(coord, out outchunk))
                    {
                        OnChunkUnLoading?.Invoke(this, chunk);
                        Lock = true;
                        Task.Run(() =>
                        {
                            if (CheckChunkExists(coord.X, coord.Y))
                            {
                                UpdateData(chunk);
                            }
                            else
                            {
                                InsertData(chunk);
                            }

                            Lock = false;
                        });
                    }
                }
            }

            if (worldMode == WorldMode.Single || worldMode == WorldMode.MultiplayerHost ||
                worldMode == WorldMode.Preview)
            {
                //创建与获取区块
                foreach (Vector2I coord in LoadingQuee.Keys)
                {
                    int max = LoadingQuee.Count;
                    WorkBase work = LoadingQuee[coord];
                    LoadingQuee.TryRemove(coord, out _);
                    Task.Run(() =>
                    {
                        Chunk chunk;
                        if (CheckChunkExists(coord.X, coord.Y))
                        {
                            chunk = GetChunkData(coord.X, coord.Y);
                            LoadedChunks[coord] = chunk;
                            if (work.Type != "NONE")
                                work.Execute(chunk);
                            if (!chunk.spawn)
                                GeneratorChunk(chunk);
                            OnChunkLoaded?.Invoke(this, chunk);
                        }
                        else
                        {
                            //生成区块
                            chunk = new(coord.X, coord.Y);
                            LoadedChunks[coord] = chunk;
                            if (work.Type != "NONE")
                                work.Execute(chunk);
                            GeneratorChunk(chunk);
                            OnChunkLoaded?.Invoke(this, chunk);
                        }
                    });
                }
            }

            if (worldMode == WorldMode.Single || worldMode == WorldMode.MultiplayerHost ||
                worldMode == WorldMode.Preview)
            {
                foreach (Vector2I coord in LoadedChunks.Keys)
                {
                    Chunk chunk = LoadedChunks[coord];
                    //chunk.Tick((WorldBase)this, world);
                }
            }


            stopwatch.Stop();
            tick_use_time = stopwatch.ElapsedMilliseconds;
        }

        public void UpdateData(Chunk chunk)
        {
            if (worldMode == WorldMode.Preview || worldMode == WorldMode.MultiplayerClient) return;
            UpdateChunkByteData(chunk.coord.X, chunk.coord.Y, MemoryPackSerializer.Serialize<Chunk>(chunk));
        }

        public void InsertData(Chunk chunk)
        {
            if (worldMode == WorldMode.Preview || worldMode == WorldMode.MultiplayerClient) return;
            InsertChunkByteValue(chunk.coord.X, chunk.coord.Y, MemoryPackSerializer.Serialize<Chunk>(chunk));
        }

        public Chunk GetChunkData(int x, int y)
        {
            if (worldMode == WorldMode.Preview)
            {
                var c = new Chunk(x, y);
                GeneratorChunk(c);
                return c;
            }

            if (worldMode == WorldMode.MultiplayerClient) return null;

            byte[] bytes = GetChunkByteData(x, y);
            Chunk chunk = MemoryPackSerializer.Deserialize<Chunk>(bytes);
            return chunk;
        }

        /// <summary>
        /// 设置方块
        /// </summary>
        /// <param name="coord">全局坐标</param>
        /// <param name="meta">方块类型</param>
        /// <param name="replaceAir">是否替换空气</param>
        public void SetBlock(Vector3I coord, BlockMeta meta, bool replaceAir = false, int state = 0)
        {
            if (worldMode == WorldMode.Preview || worldMode == WorldMode.MultiplayerClient) return;

            Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
            Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
            if (LoadedChunks.ContainsKey(ChunkCoord))
            {
                Chunk chunk = LoadedChunks[ChunkCoord];
                if (replaceAir)
                {
                    if (chunk[LocalCoord.X, LocalCoord.Y, coord.Z].IsMeta("air"))
                    {
                        chunk[LocalCoord.X, LocalCoord.Y, coord.Z] = meta.Blockdata();
                        chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                    }
                }
                else
                {
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z] = meta.Blockdata();
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                }
            }
            else
            {
                if (LoadingQuee.ContainsKey(ChunkCoord))
                {
                    if (LoadingQuee[ChunkCoord].Type == "NONE")
                    {
                        SetBlockWork sbw = new SetBlockWork()
                        {
                            Type = "SETBLOCK",
                            ExclList = new List<(Vector3I, BlockMeta, int)>(),
                        };
                        sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                        LoadingQuee[ChunkCoord] = sbw;
                    }
                    else
                    {
                        SetBlockWork stw = (SetBlockWork)LoadingQuee[ChunkCoord];
                        stw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                    }
                }
                else
                {
                    SetBlockWork sbw = new SetBlockWork()
                    {
                        Type = "SETBLOCK",
                        ExclList = new List<(Vector3I, BlockMeta, int)>(),
                    };
                    sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                    LoadingQuee[ChunkCoord] = sbw;
                }
            }
        }

        /// <summary>
        /// 获取方块,如果因为区块未加载则返回null
        /// </summary>
        /// <param name="coord">全局坐标</param>
        /// <returns>方块数据</returns>
        public Blockdata GetBlock(Vector3I coord)
        {
            Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
            Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
            if (LoadedChunks.ContainsKey(ChunkCoord))
            {
                Chunk chunk = LoadedChunks[ChunkCoord];
                return chunk[LocalCoord.X, LocalCoord.Y, coord.Z];
            }
            else
            {
                return null;
            }
        }

        public void GeneratorChunk(Chunk chunk)
        {
            WorldGenerator.Generator(chunk);
        }

        public bool GetPlayer(string name, out PlayerData playerdata)
        {
            PlayerData playerData;
            if (name == Player.LocalName)
            {
                playerData = world.player.playerData;
            }

            if (playerdatas.TryGetValue(name, out playerData))
            {
                playerdata = playerData;
                GD.Print($"[{time}] GetPlayer({name}) Done");
                return true;
            }

            if (!LoadingPlayers.Contains(name))
            {
                GD.Print($"[{time}] 玩家({name}) 加入到待加载表中");
                LoadingPlayers.Enqueue(name);
            }

            playerdata = null;
            return false;
        }

        public byte[] ChunkToByte(Chunk chunk)
        {
            var bytes = MemoryPackSerializer.Serialize<Chunk>(chunk);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }

        public static byte[] PlayerToByte(PlayerData playerdata)
        {
            byte[] bytes = MemoryPackSerializer.Serialize<PlayerData>(playerdata);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }

        public void SavePlayer(PlayerData player)
        {
            if (worldMode == WorldMode.Preview) return;
            if (worldMode == WorldMode.MultiplayerClient) return;

            var bytes = PlayerToByte(player);
            if (CheckPlayerExists(player.Name))
            {
                //GD.Print($"[{time}] UpdatePlayerByteData ({player.Name})");
                UpdatePlayerByteData(player.Name, bytes);
            }
            else
            {
                //GD.Print($"[{time}] InsertPlayerByteValue ({player.Name})");
                InsertPlayerByteValue(player.Name, bytes);
            }
        }

        public void Save()
        {
            if (worldMode == WorldMode.Preview) return;
            if (worldMode == WorldMode.MultiplayerClient) return;

            foreach (Vector2I coord in LoadedChunks.Keys)
            {
                Chunk chunk = LoadedChunks[coord];
                OnChunkUnLoading?.Invoke(this, chunk);
                if (CheckChunkExists(coord.X, coord.Y))
                {
                    UpdateData(chunk);
                }
                else
                {
                    InsertData(chunk);
                }
            }

            GD.Print($"[{time}] 保存玩家 ({playerdatas.Count})");
            foreach (var playerset in playerdatas)
            {
                SavePlayer(playerset.Value);
            }
        }
    }
}