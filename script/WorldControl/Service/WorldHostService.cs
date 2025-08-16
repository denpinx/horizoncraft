using System.Threading;
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
    private const int Port = 9999;
    private const int MaxPlayer = 16;

    public SqliteConnection sqliteConnection;

    private Task ProcessUnloadChunkTask;
    private Task ProcessLoadingChunkTask;

    public WorldHostService()
    {
    }

    public virtual bool Init()
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
        world.Multiplayer.PeerConnected += (id) => { GD.Print($"[{TickTimes}] 玩家连接"); };
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(Port, MaxPlayer);
        world.Multiplayer.MultiplayerPeer = peer;
        ServerOn = true;
        sqliteConnection = SqliteTool.InitSqlite();
        return true;
    }

    public void ProcessChunkUnloadQueue()
    {
        if (UnloadingQuee.IsEmpty) return;

        Interlocked.CompareExchange(ref ProcessUnloadChunkTask,
            Task.Run(() =>
            {
                while (UnloadingQuee.TryRemove(UnloadingQuee.Keys.FirstOrDefault(), out var chunk))
                {
                    SaveChunk(chunk);
                }
            }), null);
    }

    public virtual void UpdateLoadChunkCoords()
    {
        if (!ServerOn) return;
        LoadingChunkQuee.Clear();
        foreach (var player in Players)
        {
            SavePlayer(player.Value);
            Vector2I CenterCoord = player.Value.ChunkCoord;
            for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
            {
                for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
                {
                    Vector2I coord = new Vector2I(X, Y);
                    LoadingChunkQuee[coord] = new WorkBase();
                }
            }
        }

        foreach (Vector2I coord in LoadedChunks.Keys)
        {
            if (!LoadingChunkQuee.ContainsKey(coord))
            {
                Chunk chunk = LoadedChunks[coord];
                UnloadingQuee[coord] = chunk;
                LoadedChunks.TryRemove(coord, out _);
            }
            else
            {
                LoadingChunkQuee.TryRemove(coord, out _);
            }
        }
    }

    public void ProcessChunkLoadQueue()
    {
        if (world == null) return;
        if (LoadingChunkQuee.IsEmpty) return;

        Interlocked.CompareExchange(ref ProcessLoadingChunkTask, Task.Run((() =>
        {
            var coord = LoadingChunkQuee.Keys.FirstOrDefault();
            while (LoadingChunkQuee.TryRemove(coord, out WorkBase work))
            {
                Chunk chunk;
                if (sqliteConnection.CheckChunkExists(coord.X, coord.Y))
                {
                    var bytes = sqliteConnection.GetChunkByteData(coord.X, coord.Y);
                    chunk = MemoryPackSerializer.Deserialize<Chunk>(bytes);
                    LoadedChunks[coord] = chunk;
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
                    LoadedChunks[coord] = chunk;
                    if (work.Type != "NONE")
                        work.Execute(chunk);
                    WorldGenerator.Generator(chunk);
                    OnChunkLoaded?.Invoke(this, chunk);
                }

                coord = LoadingChunkQuee.Keys.FirstOrDefault();
            }
        })), null);
    }

    public virtual void ProcessPlayerLoadQueue()
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
                            var bytes = sqliteConnection.GetPlayerByteData(name);
                            PlayerData player = MemoryPackSerializer.Deserialize<PlayerData>(bytes);
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

    public virtual bool GetPlayer(string name, out PlayerData playerData)
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

    public virtual void SyncPlayers()
    {
        if (!ServerOn) return;
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
                    world.RpcId(pd2.PeerId, "RecivePlayer", world.player.playerData.ToByte());
                }
            }
        }
    }

    public virtual void SyncChunks()
    {
        if (!ServerOn) return;
        foreach (var chunkset in LoadedChunks)
        {
            if (chunkset.Value.update)
                foreach (var playerset in Players)
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
                            Error error = world.RpcId(pd1.PeerId, "ReciveChunk", chunk.ToByte());
                            if (error != Error.Ok)
                            {
                                GD.PrintErr(pd1.Name, pd1.PeerId);
                            }
                        }
                    }
                }
        }
    }

    public virtual void SavePlayer(PlayerData playerData)
    {
        if (!ServerOn) return;
        var bytes = playerData.ToByte();
        if (sqliteConnection.CheckPlayerExists(playerData.Name))
            sqliteConnection.UpdatePlayerByteData(playerData.Name, bytes);
        else
            sqliteConnection.InsertPlayerByteValue(playerData.Name, bytes);
    }

    public virtual void SaveChunk(Chunk chunk)
    {
        if (!ServerOn) return;
        var bytes = chunk.ToByte();
        if (sqliteConnection.CheckChunkExists(chunk.X, chunk.Y))
            sqliteConnection.UpdateChunkByteData(chunk.X, chunk.Y, bytes);
        else
            sqliteConnection.InsertChunkByteValue(chunk.X, chunk.Y, bytes);
    }

    public virtual void Save()
    {
        if (!ServerOn) return;
        foreach (var chunkset in LoadedChunks)
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
        foreach (Vector2I coord in LoadedChunks.Keys)
        {
            Chunk chunk = LoadedChunks[coord];
            chunk.Tick(this, world);
        }

        stopwatch.Stop();
        TickConsuming = stopwatch.ElapsedMilliseconds;
    }
}