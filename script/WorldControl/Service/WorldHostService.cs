using System.Threading;
using horizoncraft.script.Net;
using HorizonCraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.Tool;

namespace horizoncraft.script.WorldControl.Service;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl.work;
using MemoryPack;
using Microsoft.Data.Sqlite;

/// <summary>
/// 多人游戏，主机模式
/// 拥有全部功能
/// </summary>
public class WorldHostService : WorldBase, IWorldService, IWorldHostService, IWorldTickable
{
    //配置
    private const int Port = 9999;
    private const int MaxPlayer = 16;

    //性能监视
    //x:当前值,y:历史最大值
    public Vector2 SyncChunkTime = new Vector2();
    public Vector2 SyncPlayerTime = new Vector2();


    public SqliteConnection sqliteConnection;

    //线程管理,确保服务端后处理都在线程内完成，不阻塞主线程
    private Task ProcessUnloadChunkTask;
    private Task ProcessLoadingChunkTask;
    private Task SyncChunkTask;

    //其他异步相关的处理结果
    private ConcurrentQueue<(int, byte[])> PlayerSyncChunksCollection = new();

    public WorldHostService()
    {
    }

    public bool Init()
    {
        if (world == null) return false;
        EntityManage.Init(this);
        world.timer.Timeout += Tick;
        world.player.OnMoveToChunk += UpdateLoadChunkCoords;

        GD.Print($"[{TickTimes}] 开启服务器");

        world.Multiplayer.PeerDisconnected += (id) =>
        {
            foreach (var ps in Players)
            {
                if (ps.Value.PeerId == id)
                {
                    if (Players.TryRemove(ps.Key, out _))
                    {
                        GD.Print($"[{TickTimes}] 玩家断开连接");
                    }
                }
            }
        };
        world.Multiplayer.PeerConnected += OnPlayerJoin;
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(Port, MaxPlayer);
        world.Multiplayer.MultiplayerPeer = peer;
        ServerOn = true;
        sqliteConnection = SqliteTool.InitSqlite();
        return true;
    }

    public void ProcessChunkUnloadQueue()
    {
        if (OffloadChunkQueue.IsEmpty) return;

        Interlocked.CompareExchange(ref ProcessUnloadChunkTask,
            Task.Run(() =>
            {
                while (OffloadChunkQueue.TryRemove(OffloadChunkQueue.Keys.FirstOrDefault(), out var chunk))
                {
                    SaveChunk(chunk);
                }
            }), null);
    }

    public void UpdateLoadChunkCoords()
    {
        if (!ServerOn) return;
        LoadChunkQueue.Clear();
        foreach (var player in Players)
        {
            //SavePlayer(player.Value);
            Vector2I CenterCoord = player.Value.ChunkCoord;
            for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
            {
                for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
                {
                    Vector2I coord = new Vector2I(X, Y);
                    LoadChunkQueue[coord] = new WorkBase();
                }
            }
        }

        foreach (Vector2I coord in Chunks.Keys)
        {
            if (!LoadChunkQueue.ContainsKey(coord))
            {
                Chunk chunk = Chunks[coord];
                OffloadChunkQueue[coord] = chunk;
                Chunks.TryRemove(coord, out _);
            }
            else
            {
                LoadChunkQueue.TryRemove(coord, out _);
            }
        }
    }

    public void ProcessChunkLoadQueue()
    {
        if (world == null) return;
        if (LoadChunkQueue.IsEmpty) return;

        Interlocked.CompareExchange(ref ProcessLoadingChunkTask, Task.Run((() =>
        {
            var coord = LoadChunkQueue.Keys.FirstOrDefault();
            while (LoadChunkQueue.TryRemove(coord, out WorkBase work))
            {
                Chunk chunk;
                if (sqliteConnection.CheckChunkExists(coord.X, coord.Y))
                {
                    chunk = sqliteConnection.GetChunkByteData(coord.X, coord.Y);
                    Chunks[coord] = chunk;
                    if (work.Type != "NONE")
                        work.Execute(chunk);
                    if (!chunk.spawn)
                    {
                        WorldGenerator.Generator(chunk);
                        GD.PrintErr("异常区块重构！");
                    }

                    OnChunkLoaded?.Invoke(this, chunk);
                }
                else
                {
                    //生成区块
                    chunk = new(coord.X, coord.Y);
                    Chunks[coord] = chunk;
                    if (work.Type != "NONE")
                        work.Execute(chunk);
                    WorldGenerator.Generator(chunk);
                    OnChunkLoaded?.Invoke(this, chunk);
                }

                coord = LoadChunkQueue.Keys.FirstOrDefault();
            }
        })), null);
    }

    public void ProcessPlayerLoadQueue()
    {
        if (!ServerOn) return;
        if (LoadingPlayers.Count > 0)
        {
            new Task(() =>
            {
                for (int i = LoadingPlayers.Count - 1; i >= 0; i--)
                {
                    string name;
                    if (LoadingPlayers.TryDequeue(out name))
                    {
                        if (sqliteConnection.CheckPlayerExists(name))
                        {
                            PlayerData player = sqliteConnection.GetPlayerByteData(name);
                            player.Name = name;
                            Players[player.Name] = player;
                            GD.Print($"[{TickTimes}] 加载玩家数据:({name})");
                        }
                        else
                        {
                            PlayerData player = new PlayerData()
                            {
                                Name = name
                            };
                            if (Players.TryAdd(name, player))
                            {
                                GD.Print($"[{TickTimes}] 新建玩家数据:({name})");
                            }
                            else
                            {
                                if (!LoadingPlayers.Contains(name))
                                    LoadingPlayers.Enqueue(name);
                            }
                        }
                    }
                }
            }).Start();
        }
    }

    public bool GetPlayer(string name, out PlayerData playerData)
    {
        if (!ServerOn)
        {
            playerData = null;
            return false;
        }

        if (name == Player.LocalName && world.player.playerData != null)
        {
            playerData = world.player.playerData;
            return true;
        }

        if (Players.TryGetValue(name, out playerData))
        {
            GD.Print($"[{TickTimes}] GetPlayer({name}) Done");
            return true;
        }

        if (!LoadingPlayers.Contains(name))
        {
            GD.Print($"[{TickTimes}] 玩家({name}) 加入到待加载表中");
            LoadingPlayers.Enqueue(name);
        }

        playerData = null;
        return false;
    }

    public void SyncPlayers()
    {
        if (!ServerOn) return;

        stopwatch.Restart();
        foreach (var Fs in Players)
        {
            foreach (var Ts in Players)
            {
                if (Fs.Key != Ts.Key)
                {
                    PlayerData pd1 = Fs.Value;
                    PlayerData pd2 = Ts.Value;

                    if (
                        Math.Abs((pd1.ChunkCoord.X - pd2.ChunkCoord.X)) <= LoadHorizon &&
                        Math.Abs((pd1.ChunkCoord.Y - pd2.ChunkCoord.Y)) <= LoadHorizon
                    )
                    {
                        if (pd1.Name != Player.LocalName)
                            world.RpcId(pd1.PeerId, "UpdataPosition", pd2.Name, pd2.Position.X, pd2.Position.Y);
                    }
                }
            }
        }

        foreach (var Ts in Players)
        {
            if (Player.LocalName != Ts.Key)
            {
                PlayerData pd2 = Ts.Value;
                if (
                    Math.Abs(world.player.playerData.ChunkCoord.X - pd2.ChunkCoord.X) <= TileMapHorizon &&
                    Math.Abs(world.player.playerData.ChunkCoord.Y - pd2.ChunkCoord.Y) <= TileMapHorizon
                )
                {
                    world.RpcId(pd2.PeerId, "RecivePlayer", PlayerData.ToBytes(world.player.playerData));
                }
            }
        }

        stopwatch.Stop();
        SyncPlayerTime.X = stopwatch.ElapsedMilliseconds;
        if (SyncPlayerTime.X > SyncPlayerTime.Y) SyncPlayerTime.Y = SyncPlayerTime.X;
    }

    //同步区块
    //异步处理序列化,最终在主线程同步
    public void SyncChunks()
    {
        if (!ServerOn) return;

        stopwatch.Restart();
        Dictionary<int, PlayerSyncChunks> syncmap = new();

        Interlocked.CompareExchange(ref SyncChunkTask, Task.Run((() =>
        {
            foreach (var chunkset in Chunks)
            {
                //脏标记
                if (chunkset.Value.update_server || chunkset.Value.update_tilemap)
                {
                    Chunk chunk = chunkset.Value;
                    foreach (var playerset in Players)
                    {
                        PlayerData pd1 = playerset.Value;
                        //按距离同步
                        if (
                            Math.Abs((chunk.X - pd1.ChunkCoord.X)) <= TileMapHorizon &&
                            Math.Abs((chunk.Y - pd1.ChunkCoord.Y)) <= TileMapHorizon
                        )
                        {
                            if (pd1.Name != Player.LocalName)
                            {
                                if (syncmap.ContainsKey(pd1.PeerId))
                                {
                                    syncmap[pd1.PeerId].Chunks.Add(chunk);
                                }
                                else
                                {
                                    syncmap[pd1.PeerId] = new PlayerSyncChunks()
                                    {
                                        Chunks = new()
                                        {
                                            chunk
                                        }
                                    };
                                }
                            }
                        }
                    }

                    chunk.update_server = false;
                }
            }

            foreach (var key in syncmap.Keys)
            {
                PlayerSyncChunksCollection.Enqueue((key, PlayerSyncChunks.ToBytes(syncmap[key])));
            }
        })), null);

        if (SyncChunkTask != null && SyncChunkTask.IsCompleted && !PlayerSyncChunksCollection.IsEmpty)
        {
            foreach (var variabl in PlayerSyncChunksCollection)
            {
                world.RpcId(variabl.Item1, "ReciveChunkPack", variabl.Item2);
            }

            PlayerSyncChunksCollection.Clear();
        }


        stopwatch.Stop();
        SyncChunkTime.X = stopwatch.ElapsedMilliseconds;
        if (SyncChunkTime.X > SyncChunkTime.Y) SyncChunkTime.Y = SyncChunkTime.X;
    }

    public void OnPlayerJoin(long peer_id)
    {
        if (!ServerOn) return;
        world.RpcId(peer_id, "ReciveWorldTime", TickTimes);
    }

    public void SavePlayer(PlayerData playerData)
    {
        if (!ServerOn) return;
        if (sqliteConnection.CheckPlayerExists(playerData.Name))
            sqliteConnection.UpdatePlayerByteData(playerData.Name, playerData);
        else
            sqliteConnection.InsertPlayerByteValue(playerData.Name, playerData);
    }

    public void SaveChunk(Chunk chunk)
    {
        if (!ServerOn) return;
        if (sqliteConnection.CheckChunkExists(chunk.X, chunk.Y))
            sqliteConnection.UpdateChunkByteData(chunk.X, chunk.Y, chunk);
        else
            sqliteConnection.InsertChunkByteValue(chunk.X, chunk.Y, chunk);
    }

    public void Save()
    {
        if (!ServerOn) return;
        foreach (var chunkset in Chunks)
            SaveChunk(chunkset.Value);

        foreach (var playerset in Players)
            SavePlayer(playerset.Value);
    }

    public void Tick()
    {
        TickTimes++;
        if (!ServerOn) return;

        stopwatch.Restart();

        SyncPlayers();
        SyncChunks();
        UpdataTileMap();
        ProcessChunkLoadQueue();
        ProcessPlayerLoadQueue();
        ProcessChunkUnloadQueue();
        foreach (Vector2I coord in Chunks.Keys)
        {
            Chunk chunk = Chunks[coord];
            chunk.Tick(this, world);
        }

        stopwatch.Stop();
        TickConsuming = stopwatch.ElapsedMilliseconds;
    }
}