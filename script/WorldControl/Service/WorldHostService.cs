using System.Threading;
using horizoncraft.script.Components;
using horizoncraft.script.Inventory;
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
    private ConcurrentQueue<(int, byte[])> ChunkPacks = new();
    private ConcurrentQueue<(int, byte[])> ChunkUpdataPacks = new();
    private ConcurrentQueue<(int, byte[])> PlayerPacks = new();

    private WorldSnapshot snapshot = new WorldSnapshot();

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
        if (sqliteConnection.CheckWorldProfileExists("WorldProfile"))
        {
            Profile = sqliteConnection.GetWorldProfileByteData("WorldProfile");
            TickTimes = Profile.Time;
        }
        else
        {
            Profile = new WorldProfile();
        }

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
            for (int X = CenterCoord.X - TileMapHorizon; X <= CenterCoord.X + TileMapHorizon; X++)
            {
                for (int Y = CenterCoord.Y - TileMapHorizon; Y <= CenterCoord.Y + TileMapHorizon; Y++)
                {
                    Vector2I coord = new Vector2I(X, Y);
                    LoadChunkQueue[coord] = new WorkBase();
                }
            }
        }

        foreach (Vector2I coord in Chunks.Keys)
        {
            //当前区块不在所有玩家视野内,则保存卸载
            if (!LoadChunkQueue.ContainsKey(coord))
            {
                Chunk chunk = Chunks[coord];
                OffloadChunkQueue[coord] = chunk;
                Chunks.TryRemove(coord, out _);
            }
            else //已存在,取消加载
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

        if (name == Player.Profile.Name && world.player.playerData != null)
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
            LoadingPlayers.Enqueue(name);
        }

        playerData = null;
        return false;
    }

    public void SyncPlayers()
    {
        if (!ServerOn) return;

        stopwatch.Restart();

        Dictionary<long, PlayerPack> packs = new();
        foreach (var Fs in Players)
        {
            foreach (var Ts in Players)
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
                        if (pd1.Name != Player.Profile.Name)
                        {
                            if (!packs.ContainsKey(pd1.PeerId)) packs.Add(pd1.PeerId, new PlayerPack());
                            packs[pd1.PeerId].players.Add(PlayerdataSnapshot.ToSnapshot(pd2));
                        }
                    }
                }
            }
        }

        foreach (var sets in packs)
        {
            world.RpcId(sets.Key, "RecivePlayerDatas", ByteTool.ToBytes<PlayerPack>(sets.Value));
        }


        foreach (var player in Players.Values)
        {
            if (player.Name != Player.Profile.Name)
            {
                if (player.Inventory.update)
                {
                    player.Inventory.update = false;
                    world.RpcId(player.PeerId, "RecivePlayerInv", ByteTool.ToBytes<PlayerInventory>(player.Inventory));
                }

                if (player.OpeningBlockInventory)
                {
                    var pos = new Vector3I((int)player.OpenInventory.X, (int)player.OpenInventory.Y,
                        (int)player.OpenInventory.Z);
                    var blockdata = GetBlock(pos);
                    if (blockdata != null)
                    {
                        world.RpcId(player.PeerId, "ReciveOpenBlockData",
                            ByteTool.ToBytes<Blockdata>(blockdata),
                            ByteTool.ToBytes<PlayerInventory>(player.Inventory));
                    }
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
        // Interlocked.CompareExchange(ref SyncChunkTask, Task.Run((() =>
        // {
        Dictionary<int, ChunkPack> syncmap = new();
        Dictionary<int, WorldSnapshot> syncmap_updata = new();
        foreach (var chunk in snapshot.chunks)
        {
            //更新区块

            foreach (var playerset in Players)
            {
                PlayerData pd1 = playerset.Value;
                //按距离同步
                if (
                    Math.Abs(chunk.x - pd1.ChunkCoord.X) <= TileMapHorizon &&
                    Math.Abs(chunk.y - pd1.ChunkCoord.Y) <= TileMapHorizon
                )
                {
                    if (pd1.Name != Player.Profile.Name)
                    {
                        if (!syncmap_updata.ContainsKey(pd1.PeerId))
                            syncmap_updata[pd1.PeerId] = new WorldSnapshot();
                        syncmap_updata[pd1.PeerId].chunks.Add(chunk);
                    }
                }
            }
        }

        //脏标记
        foreach (var chunk in Chunks.Values)
            if (chunk.update_server)
            {
                foreach (var playerset in Players)
                {
                    PlayerData pd1 = playerset.Value;
                    //按距离同步
                    if (
                        Math.Abs(chunk.X - pd1.ChunkCoord.X) <= TileMapHorizon &&
                        Math.Abs(chunk.Y - pd1.ChunkCoord.Y) <= TileMapHorizon
                    )
                    {
                        if (pd1.Name != Player.Profile.Name)
                        {
                            if (syncmap.ContainsKey(pd1.PeerId))
                            {
                                syncmap[pd1.PeerId].Chunks.Add(chunk);
                            }
                            else
                            {
                                syncmap[pd1.PeerId] = new ChunkPack()
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


        foreach (var key in syncmap.Keys)
        {
            ChunkPacks.Enqueue((key, ByteTool.ToBytes<ChunkPack>(syncmap[key])));
        }

        foreach (var key in syncmap_updata.Keys)
        {
            ChunkUpdataPacks.Enqueue((key, ByteTool.ToBytes<WorldSnapshot>(syncmap_updata[key])));
        }
        //})), null);

        if (!ChunkPacks.IsEmpty)
        {
            foreach (var variabl in ChunkPacks)
            {
                world.RpcId(variabl.Item1, "ReciveChunkPack", variabl.Item2);
            }

            ChunkPacks.Clear();
        }

        if (!ChunkUpdataPacks.IsEmpty)
        {
            foreach (var variabl in ChunkUpdataPacks)
            {
                world.RpcId(variabl.Item1, "ReciveChunkUpdatePack", variabl.Item2);
            }

            ChunkUpdataPacks.Clear();
        }

        stopwatch.Stop();
        SyncChunkTime.X = stopwatch.ElapsedMilliseconds;
        if (SyncChunkTime.X > SyncChunkTime.Y) SyncChunkTime.Y = SyncChunkTime.X;
    }

    public override void SetOpenBlockComponent(PlayerData playerData, SetComponentData data)
    {
        GD.Print("修改组件！");
        var pos = new Vector3I((int)playerData.OpenInventory.X, (int)playerData.OpenInventory.Y,
            (int)playerData.OpenInventory.Z);
        
        var block = GetBlock(pos);
        if (block != null)
            ComponentManager.SetBlockComponentData(block, data);
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

    public void SaveWorldProfile(WorldProfile worldProfile)
    {
        if (!ServerOn) return;
        if (sqliteConnection.CheckWorldProfileExists("WorldProfile"))
            sqliteConnection.UpdateWorldProfileByteData("WorldProfile", worldProfile);
        else
            sqliteConnection.InsertWorldProfileByteValue("WorldProfile", worldProfile);
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
        Profile.Time = TickTimes;
        SaveWorldProfile(Profile);
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

        snapshot.chunks.Clear();
        foreach (Vector2I coord in Chunks.Keys)
        {
            Chunk chunk = Chunks[coord];
            chunk.Tick(this, world);
        }

        foreach (Vector2I coord in Chunks.Keys)
        {
            Chunk chunk = Chunks[coord];
            if (chunk.UpdateList.Count > 0)
            {
                ChunkSnapshot cs = new()
                {
                    version = TickTimes,
                    x = coord.X,
                    y = coord.Y,
                };
                foreach (var v in chunk.UpdateList)
                {
                    cs.list.Add(new BlockSnapshot()
                    {
                        x = (byte)v.X,
                        y = (byte)v.Y,
                        z = (byte)v.Z,
                        id = (short)chunk.GetBlock(v.X, v.Y, v.Z).ID,
                        state = (byte)chunk.GetBlock(v.X, v.Y, v.Z).STATE
                    });
                }

                snapshot.chunks.Add(cs);
            }
        }

        stopwatch.Stop();
        TickConsuming = stopwatch.ElapsedMilliseconds;
    }
}