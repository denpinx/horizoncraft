using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Events;
using horizoncraft.script.Expand;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.player;

public abstract class PlayerServiceBase : IDisposable
{
    public PlayerEvents Events;
    public ConcurrentDictionary<string, PlayerData> Players = new();
    public Dictionary<string, PlayerSnapshot> PlayerNodes = new();
    private ConcurrentQueue<string> _loadingqueue = new();
    private const int ProcessDleay = 100;
    protected World World;
    private PackedScene PlayerSnapshot_ps;
    private CancellationTokenSource _tokenSource;
    private Task _processTask;

    public PlayerServiceBase(World world)
    {
        this.World = world;
        world.timer.Timeout += Ticking;
        PlayerNode.GetInformation[nameof(PlayerServiceBase)] =
            () => $"在线玩家:{Players.Count}";
        PlayerSnapshot_ps = GD.Load<PackedScene>("res://tscn/PlayerSnapshot.tscn");
        Events = new PlayerEvents();
        _tokenSource = new CancellationTokenSource();
        _processTask = Task.Run(ProcessPlayerLoadThread, _tokenSource.Token);
    }

    #region 外部实现

    public virtual void Ticking()
    {
        PrecessNodeSync();
    }

    public virtual bool GetPlayerOrLoad(string name, out PlayerData playerData)
    {
        if (Players.TryGetValue(name, out var player))
        {
            GD.Print("玩家存在，返回");
            playerData = player;
            return true;
        }
        else
        {
            GD.Print($"加入队列{name}");
            _loadingqueue.Enqueue(name);
        }

        playerData = null;
        return false;
    }

    public virtual PlayerData LoadPlayer(string name)
    {
        try
        {
            using (var conn = SqliteTool.InitSqlite(World.WorldName))
            {
                if (conn.CheckPlayerExists(name))
                {
                    GD.Print("玩家存在,已加载");
                    PlayerData player = conn.GetPlayerByteData(name);
                    player.Name = name;
                    Players[player.Name] = player;
                    return player;
                }
                else
                {
                    PlayerData player = new PlayerData()
                    {
                        Name = name
                    };
                    if (Players.TryAdd(name, player))
                    {
                        return player;
                        // SearchSpawnPoint(player);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return null;
    }

    public virtual void SavePlayer(PlayerData player)
    {
        using (var conn = SqliteTool.InitSqlite(World.WorldName))
        {
            if (conn.CheckPlayerExists(player.Name))
                conn.UpdatePlayerByteData(player.Name, player);
            else
                conn.InsertPlayerByteValue(player.Name, player);
        }
    }

    public virtual void SaveAll()
    {
        using (var conn = SqliteTool.InitSqlite(World.WorldName))
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

    #endregion


    #region 内部实现

    /// <summary>
    /// 获取半径内的第一个玩家
    /// </summary>
    /// <param name="position">全局坐标</param>
    /// <param name="range">像素范围</param>
    /// <returns></returns>
    public PlayerData GetPlayerInRange(Vector2I position, int range)
    {
        foreach (var player in Players.Values)
        {
            var pos = (player.Position.ToVector2I() - position).Abs();
            if (pos.X < range && pos.Y < range)
            {
                return player;
            }
        }

        return null;
    }

    public void PrecessNodeSync()
    {
        foreach (var name in Players.Keys.ToArray())
        {
            var player = Players[name];
            if (PlayerNodes.ContainsKey(player.Name))

                PlayerNodes[player.Name].SetData(player);

            else if (!World.Service.ChunkService.Chunks.ContainsKey(player.ChunkCoord))
            {
                if (player.RemoveCount++ > 40 && player.Name != PlayerNode.Profile.Name)
                {
                    player.RemoveCount = 0;
                    SavePlayer(player);
                    Players.TryRemove(name, out _);
                    GD.Print($"玩家所在区块不存在{player.ChunkCoord}");
                }
            }
            //不给主机玩家创建对等体，且区块的视图被加载
            else if (player.Name != PlayerNode.Profile.Name) //&& WorldService.world.HasTileMap(player.ChunkCoord))
            {
                PlayerSnapshot node = PlayerSnapshot_ps.Instantiate<PlayerSnapshot>();
                PlayerNodes.Add(player.Name, node);
                World.AddChild(node);
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
                if (!World.HasTileMap(player.ChunkCoord))
                {
                    var node = PlayerNodes[name];
                    PlayerNodes.Remove(name);
                    node.QueueFree();
                }
            }
        }
    }

    public async Task ProcessPlayerLoadThread()
    {
        while (!_tokenSource.Token.IsCancellationRequested)
        {
            if (_loadingqueue.TryDequeue(out string name))
            {
                var player = LoadPlayer(name);
                if (player != null)
                {
                    GD.Print("加载玩家", name, player);
                    Players.TryAdd(name, player);
                }
            }
            else await Task.Delay(ProcessDleay, _tokenSource.Token);
        }
    }

    #endregion


    #region 其他

    public void SearchSpawnPoint(PlayerData player)
    {
        int ChunkX = Random.Shared.Next(-16, 16);
        var map = WorldGenerator.GetHighMap(ChunkX);
        int randx = Random.Shared.Next(0, Chunk.Size);
        int y = map[randx, 1];
        player.Position = new(ChunkX * Chunk.Size * 16 + randx * 16, y * 16);
        GD.Print($"寻找到复活点：{player.Position}");
    }

    public void ResetPlayerMoveStateByChunk(Vector2I coord)
    {
        foreach (var player in Players.Values)
            if (player.ChunkCoord == coord)
                player.Update = true;
    }

    public List<PlayerData> GetPlayersByRange(PlayerData from, int range)
    {
        return Players.Values.Where(player =>
                Math.Abs(from.ChunkCoord.X - player.ChunkCoord.X) <= range &&
                Math.Abs(from.ChunkCoord.Y - player.ChunkCoord.Y) <= range &&
                from.Name != player.Name)
            .ToList();
    }

    public void UpdatePlayer(PlayerDataSnapshot snapshot)
    {
        if (Players.TryGetValue(snapshot.Name, out var player))
        {
            player.Position = snapshot.GetVector2();
            player.Update = true;
            player.FaceLeft = snapshot.FaceLeft;
        }
        else
        {
            var p = new PlayerData()
            {
                Name = snapshot.Name,
                Position = snapshot.GetVector2(),
                FaceLeft = snapshot.FaceLeft,
                Update = true
            };
            Players.TryAdd(p.Name, p);
        }
    }

    #endregion

    public void Dispose()
    {
        PlayerSnapshot_ps?.Dispose();
        _tokenSource?.Dispose();
        _processTask?.Dispose();
        World.timer.Timeout -= Ticking;
    }
}