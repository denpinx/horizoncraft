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
using horizoncraft.script.WorldControl.work;
using MemoryPack;
using Microsoft.Data.Sqlite;

/// <summary>
/// 多人游戏，主机模式
/// 拥有全部功能
/// </summary>
/// 
// TODO 当前的WorldBase太重了，接下来将拆分成多个Service的组合，分别是 PlayerService,BlockService,InventoryService,EntityService(已实装)
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

    //事件

    /// <summary>
    /// 
    /// </summary>

    //线程管理,确保服务端后处理都在线程内完成，不阻塞主线程
    private Task ProcessUnloadChunkTask;

    private Task ProcessLoadingChunkTask;
    private Task SyncChunkTask;

    //其他异步相关的处理结果
    private ConcurrentQueue<(int, byte[])> ChunkPacks = new();
    private ConcurrentQueue<(int, byte[])> ChunkUpdataPacks = new();
    private ConcurrentQueue<(int, byte[])> PlayerPacks = new();

    private WorldSnapshot _worldSnapshot = new WorldSnapshot();

    public bool Init()
    {
        if (world == null) return false;
        world.timer.Timeout += Tick;

        GD.Print($"[{TickTimes}] 开启服务器");

        world.Multiplayer.PeerDisconnected += (id) =>
        {
            foreach (var ps in PlayerService.Players)
            {
                if (ps.Value.PeerId == id)
                {
                    if (PlayerService.Players.TryRemove(ps.Key, out _))
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

    /// <summary>
    /// 处理区块卸载队列
    /// </summary>
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

    /// <summary>
    /// 计算区块加载范围
    /// </summary>
    public void UpdateLoadChunkCoords()
    {
        if (!ServerOn) return;
        LoadChunkQueue.Clear();
        foreach (var player in PlayerService.Players)
        {
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

                OnChunkUnLoading(chunk);
            }
            else //已存在,取消加载
            {
                LoadChunkQueue.TryRemove(coord, out _);
            }
        }
    }

    /// <summary>
    /// 异步处理区块加载队列
    /// </summary>
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

                    OnChunkLoaded?.Invoke(chunk);
                }
                else
                {
                    //生成区块
                    chunk = new(coord.X, coord.Y);
                    Chunks[coord] = chunk;
                    if (work.Type != "NONE")
                        work.Execute(chunk);
                    WorldGenerator.Generator(chunk);
                    OnChunkLoaded?.Invoke(chunk);
                }

                coord = LoadChunkQueue.Keys.FirstOrDefault();
            }
        })), null);
    }


    /// <summary>
    /// 获取玩家信息
    /// </summary>
    /// <param name="name">玩家名</param>
    /// <param name="playerData">玩家信息</param>
    /// <returns></returns>
    public bool GetPlayer(string name, out PlayerData playerData)
    {
        if (!ServerOn)
        {
            playerData = null;
            return false;
        }

        // if (name == Player.Profile.Name && world.player.playerData != null)
        // {
        //     playerData = world.player.playerData;
        //     return true;
        // }

        if (PlayerService.GetPlayerOrLoad(name, out playerData))
        {
            return true;
        }

        playerData = null;
        return false;
    }

    public void SavePlayer(PlayerData playerData)
    {
        PlayerService.SavePlayer(playerData);
    }

    /// <summary>
    /// 同步玩家
    /// </summary>
    public void SyncPlayers()
    {
        if (!ServerOn) return;

        stopwatch.Restart();

        Dictionary<long, PlayerPack> packs = new();
        foreach (var Fs in PlayerService.Players)
        {
            PlayerData player = Fs.Value;
            if (player.Name == Player.Profile.Name) continue;
            var List = PlayerService.GetPlayersByRange(player, TileMapHorizon);
            foreach (var target in List)
            {
                if (target.Moved)
                {
                    if (!packs.ContainsKey(player.PeerId)) packs.Add(player.PeerId, new PlayerPack());
                    packs[player.PeerId].players.Add(new PlayerDataSnapshot(target));
                }
            }
        }

        //重置
        foreach (var playerData in PlayerService.Players.Values) playerData.Moved = false;

        foreach (var sets in packs)
        {
            world.RpcId(sets.Key, nameof(world.RecivePlayerDatas), ByteTool.ToBytes<PlayerPack>(sets.Value));
        }


        foreach (var player in PlayerService.Players.Values)
        {
            if (player.Name != Player.Profile.Name)
            {
                if (player.Inventory.update)
                {
                    player.Inventory.update = false;
                    world.RpcId(player.PeerId, nameof(world.RecivePlayerInv),
                        ByteTool.ToBytes<PlayerInventory>(player.Inventory));
                }

                if (player.OpeningBlockInventory)
                {
                    var pos = new Vector3I((int)player.OpenInventory.X, (int)player.OpenInventory.Y,
                        (int)player.OpenInventory.Z);
                    var blockdata = GetBlock(pos);
                    if (blockdata != null && blockdata.GetComponent<InventoryComponent>() != null)
                    {
                        world.RpcId(player.PeerId, nameof(world.ReciveOpenBlockData),
                            ByteTool.ToBytes<Blockdata>(blockdata),
                            ByteTool.ToBytes<PlayerInventory>(player.Inventory));
                    }
                    else
                    {
                        player.OpeningBlockInventory = player.OpeningBlockInventory = false;
                    }
                }
            }
        }


        stopwatch.Stop();
        SyncPlayerTime.X = stopwatch.ElapsedMilliseconds;
        if (SyncPlayerTime.X > SyncPlayerTime.Y) SyncPlayerTime.Y = SyncPlayerTime.X;
    }

    /// <summary>
    /// 同步区块
    /// </summary>
    public void SyncChunks()
    {
        if (!ServerOn) return;

        stopwatch.Restart();
        //全量更新
        Dictionary<int, ChunkPack> wholeChunkUpdate = new();
        //差异更新
        Dictionary<int, WorldSnapshot> diffUpdate = new();

        //差异更新
        foreach (var chunk in _worldSnapshot.chunks)
        {
            foreach (var playerset in PlayerService.Players)
            {
                PlayerData pd1 = playerset.Value;
                //按距离同步
                if (
                    Math.Abs(chunk.X - pd1.ChunkCoord.X) <= TileMapHorizon &&
                    Math.Abs(chunk.Y - pd1.ChunkCoord.Y) <= TileMapHorizon
                )
                {
                    if (!diffUpdate.ContainsKey(pd1.PeerId))
                        diffUpdate[pd1.PeerId] = new WorldSnapshot();
                    diffUpdate[pd1.PeerId].chunks.Add(chunk);
                    chunk.ResetEmptyOwned(pd1.Name); //更新实体的从属
                }
            }
        }

        //全量更新
        foreach (var chunk in Chunks.Values)
            if (chunk.update_server)
            {
                //下一帧同步这个区块内的玩家
                PlayerService.ResetPlayerMoveStateByChunk(chunk.coord);
                foreach (var playerset in PlayerService.Players)
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
                            if (wholeChunkUpdate.ContainsKey(pd1.PeerId))
                            {
                                wholeChunkUpdate[pd1.PeerId].Chunks.Add(chunk);
                            }
                            else
                            {
                                var result = EntityService.GetEntityByChunk(chunk.coord);
                                chunk.Entitys = result;

                                wholeChunkUpdate[pd1.PeerId] = new ChunkPack()
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


        foreach (var key in wholeChunkUpdate.Keys)
        {
            ChunkPacks.Enqueue((key, ByteTool.ToBytes<ChunkPack>(wholeChunkUpdate[key])));
        }

        foreach (var key in diffUpdate.Keys)
        {
            if (key != 0)
                ChunkUpdataPacks.Enqueue((key, ByteTool.ToBytes<WorldSnapshot>(diffUpdate[key])));
        }


        //})), null);

        if (!ChunkPacks.IsEmpty)
        {
            foreach (var variabl in ChunkPacks)
            {
                world.RpcId(variabl.Item1, nameof(world.ReciveChunkPack), variabl.Item2);
            }

            ChunkPacks.Clear();
        }

        if (!ChunkUpdataPacks.IsEmpty)
        {
            foreach (var variabl in ChunkUpdataPacks)
            {
                world.RpcId(variabl.Item1, nameof(world.ReciveChunkUpdatePack), variabl.Item2);
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
            ComponentManager.SetBlockComponentData(playerData, block, data);
    }

    /// <summary>
    /// 当玩家加入游戏时同步时间
    /// </summary>
    /// <param name="peer_id"></param>
    public void OnPlayerJoin(long peer_id)
    {
        if (!ServerOn) return;
        world.RpcId(peer_id, nameof(world.ReciveWorldTime), TickTimes);
    }

    /// <summary>
    /// 保存世界文档
    /// </summary>
    /// <param name="worldProfile"></param>
    public void SaveWorldProfile(WorldProfile worldProfile)
    {
        if (!ServerOn) return;
        if (sqliteConnection.CheckWorldProfileExists("WorldProfile"))
            sqliteConnection.UpdateWorldProfileByteData("WorldProfile", worldProfile);
        else
            sqliteConnection.InsertWorldProfileByteValue("WorldProfile", worldProfile);
    }

    /// <summary>
    /// 保存所有区块
    /// </summary>
    /// <param name="chunk"></param>
    public void SaveChunk(Chunk chunk)
    {
        if (!ServerOn) return;
        if (sqliteConnection.CheckChunkExists(chunk.X, chunk.Y))
            sqliteConnection.UpdateChunkByteData(chunk.X, chunk.Y, chunk);
        else
            sqliteConnection.InsertChunkByteValue(chunk.X, chunk.Y, chunk);
    }

    /// <summary>
    /// 执行所有存档操作
    /// </summary>
    public void Save()
    {
        if (!ServerOn) return;
        foreach (var chunkset in Chunks)
            SaveChunk(chunkset.Value);

        PlayerService.SavePlayers();
        Profile.Time = TickTimes;
        SaveWorldProfile(Profile);
    }

    /// <summary>
    /// 时刻更新
    /// 20Tick/s
    /// </summary>
    public void Tick()
    {
        TickTimes++;
        if (!ServerOn) return;

        stopwatch.Restart();
        UpdateLoadChunkCoords();
        SyncPlayers();
        SyncChunks();
        ProcessChunkLoadQueue();
        ProcessChunkUnloadQueue();

        _worldSnapshot.chunks.Clear();
        foreach (Vector2I coord in Chunks.Keys)
        {
            Chunk chunk = Chunks[coord];
            chunk.Tick(this, world);
        }

        foreach (Vector2I coord in Chunks.Keys)
        {
            Chunk chunk = Chunks[coord];
            ChunkSnapshot cs = null;
            bool IsUpdate = false;
            if (chunk.UpdateList.Count > 0)
            {
                IsUpdate = true;
                cs = new()
                {
                    Version = TickTimes,
                    X = coord.X,
                    Y = coord.Y,
                };


                foreach (var v in chunk.UpdateList)
                {
                    cs.list.Add(new BlockSnapshot()
                    {
                        x = (byte)v.X,
                        y = (byte)v.Y,
                        z = (byte)v.Z,
                        id = (short)chunk.GetBlock(v.X, v.Y, v.Z).Id,
                        state = (byte)chunk.GetBlock(v.X, v.Y, v.Z).State
                    });
                }

                _worldSnapshot.chunks.Add(cs);
            }

            if (cs == null && !IsUpdate)
            {
                cs = new()
                {
                    Version = TickTimes,
                    X = coord.X,
                    Y = coord.Y,
                };
                var result = EntityService.GetChunkMovedEntity(coord);
                if (result.Count > 0)
                {
                    GD.Print($"【服务端】获取了 {result.Count} 个实体更新");
                    cs.Entiydatas = result;
                }

                _worldSnapshot.chunks.Add(cs);
            }
        }

        OnTicked?.Invoke();

        UpdateLights();
        UpdataTileMap();

        stopwatch.Stop();
        TickConsuming = stopwatch.ElapsedMilliseconds;
    }
}