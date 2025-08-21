using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;
using MemoryPack;
using Microsoft.Data.Sqlite;

namespace HorizonCraft.script.WorldControl.Service;

/// <summary>
/// 预览模式
/// 只创建区块并预览效果，不保存和加载任何数据
/// </summary>
public class WorldPreviewService : WorldBase, IWorldService, IWorldTickable
{
    public bool Init()
    {
        if (world == null) return false;
        EntityManage.Init(this);
        world.timer.Timeout += Tick;
        world.player.OnMoveToChunk += UpdateLoadChunkCoords;
        GD.Print($"[{TickTimes}] 开启预览模式");
        return true;
    }

    public void ProcessChunkUnloadQueue()
    {
    }

    public void UpdateLoadChunkCoords()
    {
        if (world == null) return;
        if (world.player.playerData == null) return;
        LoadChunkQueue.Clear();
        
        GD.Print("移动");
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
        foreach (Vector2I coord in LoadChunkQueue.Keys)
        {
            int max = LoadChunkQueue.Count;
            WorkBase work = LoadChunkQueue[coord];
            LoadChunkQueue.TryRemove(coord, out _);
            Task.Run(() =>
            {
                //生成区块
                Chunk chunk = new(coord.X, coord.Y);
                Chunks[coord] = chunk;
                if (work.Type != "NONE")
                    work.Execute(chunk);
                WorldGenerator.Generator(chunk);
                OnChunkLoaded?.Invoke(this, chunk);
            });
        }
    }

    public void ProcessPlayerLoadQueue()
    {
    }

    public bool GetPlayer(string name, out PlayerData playerdata)
    {
        playerdata = new PlayerData() { Name = name };
        return true;
    }

    public void SavePlayer(PlayerData playerData)
    {
    }

    public void SaveChunk(Chunk chunk)
    {
    }

    public void Save()
    {
    }

    public void Tick()
    {
        TickTimes++;
        stopwatch.Restart();
        ProcessChunkLoadQueue();
        UpdataTileMap();
        foreach (Vector2I coord in Chunks.Keys)
        {
            Chunk chunk = Chunks[coord];
            chunk.Tick(this, world);
        }

        stopwatch.Stop();
        TickConsuming = stopwatch.ElapsedMilliseconds;
    }
}