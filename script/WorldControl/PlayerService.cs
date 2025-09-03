using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.Tool;
using Microsoft.Data.Sqlite;

namespace horizoncraft.script.WorldControl;

public class PlayerService : IDisposable
{
    public ConcurrentDictionary<string, PlayerData> Players = new();

    public Dictionary<string, PlayerSnapshot> PlayerNodes = new();

    private ConcurrentQueue<string> _loadingqueue = new();
    private bool IsClient = false;
    private WorldBase WorldService;
    private CancellationTokenSource TokenSource;
    private PackedScene PlayerSnapshot_ps;
    private Task _ProcessTask;

    /// <summary>
    /// 空处理时的冻结秒
    /// </summary>
    const int ProcessDleay = 100;


    public PlayerService(WorldBase worldBase)
    {
        this.WorldService = worldBase;
        WorldService.OnTicked += PrecessNodSync;
        if (worldBase is WorldClientService)
            IsClient = true;
        Player.GetInformation["PlayerService"] = Lambda;

        PlayerSnapshot_ps = GD.Load<PackedScene>("res://tscn/PlayerSnapshot.tscn");


        TokenSource = new CancellationTokenSource();
        _ProcessTask = Task.Run(AsynProcessLoadingQueue, TokenSource.Token);
        return;
        string Lambda() => $"加载玩家：{Players.Count}\n待加载玩家：{_loadingqueue.Count}";
    }

    /// <summary>
    /// 主线程同步
    /// </summary>
    private void PrecessNodSync()
    {
        foreach (var name in Players.Keys.ToArray())
        {
            var player = Players[name];
            //更新
            if (PlayerNodes.ContainsKey(player.Name))
            {
                PlayerNodes[player.Name].SetData(player);
            }
            // TODO 被注释的代码有问题，和区块加载循环冲突了，区块加载要依靠玩家数据，玩家数据又要依靠区块加载，这样写行不通
            //区块不存在，出现这种区块只有玩家离线
            else if (!WorldService.Chunks.ContainsKey(player.ChunkCoord))
            {
                if (player.RemoveCount < 10) player.RemoveCount++;
                else
                {
                    //保存卸载玩家
                    player.RemoveCount = 0;
                    SavePlayer(player);
                    Players.TryRemove(name, out _);
                    GD.Print($"玩家所在区块不存在{player.ChunkCoord}");
                }
            }
            //不给主机玩家创建对等体，且区块的视图被加载
            else if (player.Name != Player.Profile.Name) //&& WorldService.world.HasTileMap(player.ChunkCoord))
            {
                PlayerSnapshot node = PlayerSnapshot_ps.Instantiate<PlayerSnapshot>();
                PlayerNodes.Add(player.Name, node);
                WorldService.world.AddChild(node);
                //TODO 如果报错了就删掉下面这一行
                node.SetData(player);
            }
        }

        //数据不存在或则区块的视图没有加载则删除
        foreach (var name in PlayerNodes.Keys.ToArray())
        {
            if (!Players.ContainsKey(name))
            {
                var node = PlayerNodes[name];
                PlayerNodes.Remove(name);
                node.QueueFree();
            }
            else
            {
                var player = Players[name];
                if (!WorldService.world.HasTileMap(player.ChunkCoord))
                {
                    var node = PlayerNodes[name];
                    PlayerNodes.Remove(name);
                    node.QueueFree();
                }
            }
        }
    }


    /// <summary>
    /// 异步处理线程
    /// </summary>
    private async Task AsynProcessLoadingQueue()
    {
        while (!TokenSource.Token.IsCancellationRequested)
        {
            if (_loadingqueue.TryDequeue(out string name))
            {
                await LoadPlayerAsync(name);
            }
            else await Task.Delay(ProcessDleay, TokenSource.Token);
        }
    }

    /// <summary>
    /// 异步加载玩家
    /// </summary>
    /// <param name="name"></param>
    private async Task LoadPlayerAsync(string name)
    {
        try
        {
            using (var conn = SqliteTool.InitSqlite())
            {
                if (conn.CheckPlayerExists(name))
                {
                    PlayerData player = conn.GetPlayerByteData(name);
                    player.Name = name;
                    Players[player.Name] = player;
                    WorldService.OnPlayerJoinGame?.Invoke(player);
                }
                else
                {
                    PlayerData player = new PlayerData()
                    {
                        Name = name
                    };
                    if (Players.TryAdd(name, player))
                    {
                        WorldService.OnPlayerFirstJoinGame?.Invoke(player);
                        SearchSpawnPoint(player);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    /// <summary>
    /// 重置这个区块内的玩家更新
    /// 通常在玩家移动时请求更新区块时调用,以便重新更新玩家的状态
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public void ResetPlayerMoveStateByChunk(Vector2I coord)
    {
        foreach (var player in Players.Values)
            if (player.ChunkCoord == coord)
                player.Moved = true;
    }

    /// <summary>
    /// 获取区块内的实体
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public List<PlayerData> GetPlayersByChunk(Vector2I coord)
    {
        List<PlayerData> result = new();
        foreach (var player in Players.Values)
            if (player.ChunkCoord == coord)
                result.Add(player);
        return result;
    }

    /// <summary>
    /// 获取区块半径内的所有玩家
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public List<PlayerData> GetPlayersByRange(PlayerData from, int range)
    {
        return Players.Values.Where(player =>
                Math.Abs(from.ChunkCoord.X - player.ChunkCoord.X) <= range &&
                Math.Abs(from.ChunkCoord.Y - player.ChunkCoord.Y) <= range &&
                from.Name != player.Name)
            .ToList();
    }

    /// <summary>
    /// 获取玩家或加载
    /// </summary>
    /// <param name="name"></param>
    /// <param name="playerData"></param>
    /// <returns></returns>
    public bool GetPlayerOrLoad(string name, out PlayerData playerData)
    {
        if (Players.TryGetValue(name, out var player))
        {
            playerData = player;
            return true;
        }
        else if (!IsClient)
        {
            _loadingqueue.Enqueue(name);
        }

        playerData = null;
        return false;
    }

    /// <summary>
    /// 通过快照更新已加载玩家
    /// 注意这个是双向的，服务端和客户端都是通过这个更新
    /// </summary>
    /// <param name="player"></param>
    public void UpdatePlayer(PlayerDataSnapshot player)
    {
        if (Players.TryGetValue(player.Name, out var playerData))
        {
            var pos = player.GetVector2();
            if (playerData.Position != pos)
                playerData.Moved = true;
            playerData.Position = pos;
        }
        else
        {
            PlayerData data = new PlayerData()
            {
                Name = player.Name,
                Position = player.GetVector2(),
                FaceLeft = player.FaceLeft,
                Moved = false,
            };
            Players.TryAdd(player.Name, data);
        }
    }

    /// <summary>
    /// 寻找复活点，通常是玩家死亡时或则第一次加入游戏时调用
    /// </summary>
    /// <param name="player">玩家</param>
    public void SearchSpawnPoint(PlayerData player)
    {
        int ChunkX = Random.Shared.Next(-16, 16);
        var map = WorldGenerator.GetHighMap(ChunkX);
        int randx = Random.Shared.Next(0, Chunk.Size);
        int y = map[randx, 1];
        player.Position = new(ChunkX * Chunk.Size * 16 + randx * 16, y * 16);
        GD.Print($"寻找到复活点：{player.Position}");
    }

    /// <summary>
    /// 保存玩家
    /// </summary>
    /// <param name="player"></param>
    public void SavePlayer(PlayerData player)
    {
        using (var conn = SqliteTool.InitSqlite())
        {
            if (conn.CheckPlayerExists(player.Name))
                conn.UpdatePlayerByteData(player.Name, player);
            else
                conn.InsertPlayerByteValue(player.Name, player);
        }
    }

    public void SavePlayers()
    {
        using (var conn = SqliteTool.InitSqlite())
        {
            foreach (var player in Players.Values)
            {
                if (conn.CheckPlayerExists(player.Name))
                    conn.UpdatePlayerByteData(player.Name, player);
                else
                    conn.InsertPlayerByteValue(player.Name, player);
            }
        }
    }

    public void Dispose()
    {
        TokenSource.Cancel();
        _ProcessTask?.Wait(1000);
        TokenSource.Dispose();
    }
}