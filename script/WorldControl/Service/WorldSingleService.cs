using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Net;
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
public class WorldSingleService : WorldBase, IWorldService, IWorldTickable
{
    public SqliteConnection sqliteConnection;

    private Task ProcessUnloadChunkTask;
    private Task ProcessLoadingChunkTask;
    private Task ProcessLoadingPlayerTask;

    public bool Init()
    {
        if (world == null) return false;
        world.timer.Timeout += Tick;
        world.player.OnMoveToChunk += UpdateLoadChunkCoords;
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

        GD.Print("【单人游戏】 初始化成功");
        return true;
    }

    public void UpdateLoadChunkCoords()
    {
        if (world == null) return;
        LoadChunkQueue.Clear();

        if (world.player.playerData != null) SavePlayer(world.player.playerData);
        else return;
        Vector2I CenterCoord = world.player.playerData.ChunkCoord;
        for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
        {
            for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
            {
                Vector2I coord = new Vector2I(X, Y);
                LoadChunkQueue[coord] = new WorkBase();
            }
        }


        foreach (Vector2I coord in Chunks.Keys)
        {
            if (!LoadChunkQueue.ContainsKey(coord))
            {
                Chunk chunk = Chunks[coord];
                OffloadChunkQueue[coord] = chunk;
                Chunks.TryRemove(coord, out _);
                OnChunkUnLoading(chunk);
            }
            else
            {
                LoadChunkQueue.TryRemove(coord, out _);
            }
        }
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

                //UpdataChunkLight(chunk);
                coord = LoadChunkQueue.Keys.FirstOrDefault();
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

        // if (name == Player.Profile.Name && world.player.playerData != null)
        // {
        //     playerdata = world.player.playerData;
        //     return true;
        // }

        if (PlayerService.GetPlayerOrLoad(name, out playerdata))
        {
            return true;
        }
        playerdata = null;
        return false;
    }

    public void SavePlayer(PlayerData playerData)
    {
        if (world == null) return;
        if (sqliteConnection.CheckPlayerExists(playerData.Name))
            sqliteConnection.UpdatePlayerByteData(playerData.Name, playerData);
        else
            sqliteConnection.InsertPlayerByteValue(playerData.Name, playerData);
    }

    public void SaveChunk(Chunk chunk)
    {
        if (world == null) return;
        if (sqliteConnection.CheckChunkExists(chunk.X, chunk.Y))
            sqliteConnection.UpdateChunkByteData(chunk.X, chunk.Y, chunk);
        else
            sqliteConnection.InsertChunkByteValue(chunk.X, chunk.Y, chunk);
    }

    public void SaveWorldProfile(WorldProfile worldProfile)
    {
        if (!ServerOn) return;
        if (sqliteConnection.CheckWorldProfileExists("WorldProfile"))
            sqliteConnection.UpdateWorldProfileByteData("WorldProfile", worldProfile);
        else
            sqliteConnection.InsertWorldProfileByteValue("WorldProfile", worldProfile);
    }

    public void Save()
    {
        if (world == null) return;
        foreach (var chunkset in Chunks)
            SaveChunk(chunkset.Value);

        foreach (var playerset in PlayerService.Players)
            SavePlayer(playerset.Value);
        Profile.Time = TickTimes;
        SaveWorldProfile(Profile);
    }

    public void Tick()
    {
        TickTimes++;
        stopwatch.Restart();

        ProcessChunkLoadQueue();
        ProcessPlayerLoadQueue();
        ProcessChunkUnloadQueue();

        foreach (Vector2I coord in Chunks.Keys.ToArray())
        {
            Chunk chunk = Chunks[coord];
            chunk.Tick(this, world);
        }

        OnTicked?.Invoke();

        UpdateLights();
        UpdataTileMap();


        stopwatch.Stop();
        TickConsuming = stopwatch.ElapsedMilliseconds;
    }
}