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
using horizoncraft.script.Services;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.player;

/// <summary>
/// 基础玩家服务
/// 拥有功能：
///     玩家管理
///     生命周期管理
///     玩家事件处理
///     异步玩家资源加载
/// </summary>
public abstract class PlayerServiceBase : ServiceBase, IDisposable, ISave
{
    /// <summary>
    /// 玩家事件处理
    /// </summary>
    public PlayerEvents Events;

    /// <summary>
    /// 已加载玩家集合
    /// </summary>
    public ConcurrentDictionary<string, PlayerData> Players = new();

    /// <summary>
    /// 已加载的玩家节点集合
    /// </summary>
    public Dictionary<string, PlayerSnapshot> PlayerNodes = new();

    /// <summary>
    /// 待加载或创建的玩家数据集合
    /// </summary>
    private ConcurrentQueue<string> _loadingqueue = new();

    /// <summary>
    /// 每次加载的等待延迟
    /// </summary>
    private const int ProcessDleay = 100;

    private PackedScene PlayerSnapshot_ps = GD.Load<PackedScene>("res://tscn/PlayerSnapshot.tscn");
    private CancellationTokenSource _tokenSource;
    private Task _processTask;

    public PlayerServiceBase(World world) : base(world)
    {
        world.timer.Timeout += Ticking;
        PlayerNode.GetInformation[nameof(PlayerServiceBase)] =
            () => $"在线玩家:{Players.Count}";
        Events = new PlayerEvents();
        _tokenSource = new CancellationTokenSource();
        _processTask = Task.Run(ProcessPlayerLoadThread, _tokenSource.Token);
    }

    #region 外部实现

    /// <summary>
    /// 时刻处理
    /// </summary>
    public virtual void Ticking()
    {
        PrecessNodeSync();
        ProcessPlayerState();
        if (World.PlayerNode.playerData != null)
            SyncPlayerNodePositionToData(World.PlayerNode, World.PlayerNode.playerData);
    }

    /// <summary>
    /// 处理玩家状态
    /// </summary>
    public virtual void ProcessPlayerState()
    {
        foreach (var player in Players.Values)
        {
            if (player.State == PlayerState.Live)
            {
                //预期每两秒掉落1的血量   
                if (player.Hunger.Value <= 0f)
                {
                    if (Random.Shared.Next(40) == 0)
                    {
                        player.Health.Value -= 1f;
                    }
                }

                if (player.Health.Value <= 0)
                {
                    player.State = PlayerState.Dead;
                }
            }

            //复活中
            if (player.State == PlayerState.Respawning)
            {
                if (player.HasSpawnPoint)
                {
                    Vector2 pos = player.SpawnPoint.ToGodotVector2();
                    if (TrySearchSpawn(player, pos.ToVector2I()))
                    {
                        player.State = PlayerState.WaitSpawn;
                        player.Update = true;
                        OnPlayerRespawn(player);
                    }
                }
                else
                {
                    if (SearchSpawnPoint(player))
                    {
                        player.State = PlayerState.WaitSpawn;
                        player.Update = true;
                        OnPlayerRespawn(player);
                    }
                }
            }
        }
    }

    public virtual void OnPlayerRespawn(PlayerData playerData)
    {
    }

    /// <summary>
    /// 获取玩家或加入待加载列表，使用方法是定时调用直到被加载或创建
    /// </summary>
    /// <param name="name">玩家名称</param>
    /// <param name="playerData">玩家数据</param>
    /// <returns>该玩家是否已存在</returns>
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

    /// <summary>
    /// 加载玩家
    /// </summary>
    /// <param name="name">玩家名</param>
    /// <returns>玩家数据</returns>
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

    /// <summary>
    /// 同步玩家的节点坐标和数据坐标
    /// 决定狙击玩家或则客户端玩家的同步方法的。
    /// </summary>
    /// <param name="player"></param>
    /// <param name="playerData"></param>
    public virtual void SyncPlayerNodePositionToData(PlayerNode player, PlayerData playerData)
    {
        if (World.Service.ChunkService.Chunks.ContainsKey(playerData.ChunkCoord))
        {
            if (playerData.Mode == 0 && player.Stop)
            {
                player.Stop = false;
            }
        }
        else
        {
            player.Stop = true;
        }

        if (playerData.State == PlayerState.Live)
        {
            var pos = player.Position.ToSystemVector2();
            if (pos != playerData.Position)
            {
                playerData.Position = pos;
                playerData.Update = true;
            }
        }

        if (playerData.State == PlayerState.WaitSpawn)
        {
            player.Position = playerData.Position_v2;
            playerData.State = PlayerState.Live;
        }
    }

    /// <summary>
    /// 保存玩家
    /// </summary>
    /// <param name="player">玩家数据</param>
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

    /// <summary>
    /// 持久化接口实现
    /// </summary>
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

    /// <summary>
    /// 处理玩家节点同步
    /// </summary>
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

    /// <summary>
    /// 处理玩家加载的持久线程
    /// </summary>
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

    /// <summary>
    /// 搜寻随机复活点
    /// </summary>
    /// <param name="player">玩家数据</param>
    public virtual bool SearchSpawnPoint(PlayerData player)
    {
        int ChunkX = Random.Shared.Next(-16, 16);
        var map = WorldGenerator.GetHighMap(ChunkX);
        int randx = Random.Shared.Next(0, Chunk.Size);
        int gy = map[randx, 1];

        return TrySearchSpawn(player, new Vector2I(ChunkX * Chunk.Size + randx, gy));
    }

    public virtual bool TrySearchSpawn(PlayerData player, Vector2I position)
    {
        var local = position.Remainder(Chunk.Size);
        var coord = position.MathFloor(Chunk.Size);

        bool Found = true;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var pos = new Vector2I(coord.X + local.X + x, coord.Y + local.Y + y);
                if (World.Service.ChunkService.Chunks.ContainsKey(pos))
                {
                }
                else
                {
                    if (Found)
                        Found = false;
                    World.Service.ChunkService.LoadChunkQueue.Add(pos);
                }
            }
        }

        if (Found)
        {
            //自中心向四周查询
            for (int i = 0; i < Chunk.Size; i++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    if (FundPoint(i, y)) return true;
                    if (FundPoint(-i, y)) return true;

                    if (FundPoint(i, -y)) return true;
                    if (FundPoint(-i, -y)) return true;
                }
            }
        }

        return false;

        bool FundPoint(int x, int y)
        {
            int gx = coord.X * Chunk.Size + x;
            int gy = coord.Y * Chunk.Size + y;
            var block = World.Service.ChunkService.GetBlock(new Vector3I(gx, gy, 1));
            if (block != null)
            {
                if (!block.BlockMeta.Collide)
                {
                    var down = World.Service.ChunkService.GetBlock(new Vector3I(gx, gy + 1, 1));
                    if (down != null && down.BlockMeta.Collide)
                    {
                        player.Position = new(gx * 16, gy * 16 - 16);
                        GD.Print($"寻找到复活点: {player.Position.ToString()}");
                        GD.Print($"方块坐标: {gx},{gy}");
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 重置指定区块内的所有玩家的更新状态
    /// </summary>
    /// <param name="coord"></param>
    public void ResetPlayerMoveStateByChunk(Vector2I coord)
    {
        foreach (var player in Players.Values)
            if (player.ChunkCoord == coord)
                player.Update = true;
    }

    /// <summary>
    /// 获取指定玩家区块半径内的其它玩家集合
    /// </summary>
    /// <param name="from"></param>
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
    /// 用玩家快照数据更新玩家数据。来源可能是服务端，也可能是客户端
    /// </summary>
    /// <param name="snapshot">玩家快照信息</param>
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