using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.Tool;
using MemoryPack;
using Microsoft.Data.Sqlite;

namespace HorizonCraft.script.WorldControl.Service;

/// <summary>
/// 单机模式
/// 拥有全部服务器功能
/// </summary>
public class WorldSingleService : WorldBase, IWorldService, IBaseManage, IWorldTickable
{
    public SqliteConnection sqliteConnection;

    private Task ProcessUnloadChunkTask;
    private Task ProcessLoadingChunkTask;
    private Task ProcessLoadingPlayerTask;

    public bool Init()
    {
        if (world == null) return false;
        EntityManage.Init(this);
        world.timer.Timeout += Tick;
        world.player.OnMoveToChunk += UpdateLoadChunkCoords;
        sqliteConnection = SqliteTool.InitSqlite();

        GD.Print("【单人游戏】 初始化成功");
        return true;
    }

    public void UpdateLoadChunkCoords()
    {
        if (world == null) return;
        LoadingChunkQuee.Clear();

        if (world.player.playerData != null) SavePlayer(world.player.playerData);
        else return;
        Vector2I CenterCoord = world.player.playerData.ChunkCoord;
        for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
        {
            for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
            {
                Vector2I coord = new Vector2I(X, Y);
                LoadingChunkQuee[coord] = new WorkBase();
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

    public void ProcessPlayerLoadQueue()
    {
        if (world == null) return;
    }

    public bool GetPlayer(string name, out PlayerData playerdata)
    {
        if (world == null || sqliteConnection == null)
        {
            playerdata = null;
            return false;
        }

        if (name == Player.LocalName && world.player.playerData != null)
        {
            playerdata = world.player.playerData;
            return true;
        }

        if (Players.TryGetValue(name, out playerdata))
        {
            GD.Print($"[{TickTimes}] GetPlayer({name}) Done");
            return true;
        }

        if (sqliteConnection.CheckPlayerExists(Player.LocalName))
        {
            var bytes = sqliteConnection.GetPlayerByteData(Player.LocalName);
            PlayerData player = MemoryPackSerializer.Deserialize<PlayerData>(bytes);
            player.Name = Player.LocalName;
            Players[player.Name] = player;
            GD.Print($"[{TickTimes}] 加载玩家数据:({Player.LocalName})");
            LoadingPlayers.Clear();
        }
        else
        {
            PlayerData player = new PlayerData()
            {
                Name = Player.LocalName
            };
            Players[Player.LocalName] = player;
            GD.Print($"[{TickTimes}] 新建玩家数据:({Player.LocalName})");
            LoadingPlayers.Clear();
        }

        playerdata = null;
        return false;
    }

    public void SavePlayer(PlayerData playerData)
    {
        if (world == null) return;
        var bytes = playerData.ToByte();
        if (sqliteConnection.CheckPlayerExists(playerData.Name))
            sqliteConnection.UpdatePlayerByteData(playerData.Name, bytes);
        else
            sqliteConnection.InsertPlayerByteValue(playerData.Name, bytes);
    }

    public void SaveChunk(Chunk chunk)
    {
        if (world == null) return;
        var bytes = chunk.ToByte();
        if (sqliteConnection.CheckChunkExists(chunk.X, chunk.Y))
            sqliteConnection.UpdateChunkByteData(chunk.X, chunk.Y, bytes);
        else
            sqliteConnection.InsertChunkByteValue(chunk.X, chunk.Y, bytes);
    }

    public void Save()
    {
        if (world == null) return;
        foreach (var chunkset in LoadedChunks)
            SaveChunk(chunkset.Value);

        foreach (var playerset in Players)
            SavePlayer(playerset.Value);
    }

    public void Tick()
    {
        TickTimes++;
        stopwatch.Restart();

        ProcessChunkLoadQueue();
        ProcessPlayerLoadQueue();
        UpdataTileMap();

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