using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.work;
using Microsoft.Data.Sqlite;

namespace HorizonCraft.script.WorldControl.Service;

public class WorldBase
{
    public Action<WorldBase, Chunk> OnChunkLoaded;

    public Action<WorldBase, Chunk> OnChunkUnLoading;

    //
    public bool Connect = false;
    public bool ServerOn = false;
    public long TickTimes;
    public long TickConsuming;
    public int LoadHorizon = 3;
    public int TileMapHorizon = 2;
    public Stopwatch stopwatch = new Stopwatch();

    /// <summary>
    /// 已加载区块
    /// </summary>
    public ConcurrentDictionary<Vector2I, Chunk> LoadedChunks = new();

    /// <summary>
    /// 已加载玩家
    /// </summary>
    public ConcurrentDictionary<string, PlayerData> Players = new();

    /// <summary>
    /// 待卸载区块
    /// </summary>
    public ConcurrentDictionary<Vector2I, Chunk> UnloadingQuee = new();

    /// <summary>
    /// 待加载玩家
    /// </summary>
    public ConcurrentQueue<string> LoadingPlayers = new();

    /// <summary>
    /// 待加载区块
    /// </summary>
    public ConcurrentDictionary<Vector2I, WorkBase> LoadingChunkQuee = new();

    public bool Lock = false;
    public World world;

    public WorldBase()
    {
    }

    public virtual void SetBlock(Vector3I coord, BlockMeta meta, bool replaceAir = false, int state = 0)
    {
        Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
        Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
        if (LoadedChunks.ContainsKey(ChunkCoord))
        {
            Chunk chunk = LoadedChunks[ChunkCoord];
            if (replaceAir)
            {
                if (chunk[LocalCoord.X, LocalCoord.Y, coord.Z].IsMeta("air"))
                {
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z] = meta.Blockdata();
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                }
            }
            else
            {
                chunk[LocalCoord.X, LocalCoord.Y, coord.Z] = meta.Blockdata();
                chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
            }
        }
        else
        {
            if (LoadingChunkQuee.ContainsKey(ChunkCoord))
            {
                if (LoadingChunkQuee[ChunkCoord].Type == "NONE")
                {
                    SetBlockWork sbw = new SetBlockWork()
                    {
                        Type = "SETBLOCK",
                        ExclList = new List<(Vector3I, BlockMeta, int)>(),
                    };
                    sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                    LoadingChunkQuee[ChunkCoord] = sbw;
                }
                else
                {
                    SetBlockWork stw = (SetBlockWork)LoadingChunkQuee[ChunkCoord];
                    stw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                }
            }
            else
            {
                SetBlockWork sbw = new SetBlockWork()
                {
                    Type = "SETBLOCK",
                    ExclList = new List<(Vector3I, BlockMeta, int)>(),
                };
                sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                LoadingChunkQuee[ChunkCoord] = sbw;
            }
        }
    }

    public virtual Blockdata GetBlock(Vector3I coord)
    {
        Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
        Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
        if (LoadedChunks.ContainsKey(ChunkCoord))
        {
            Chunk chunk = LoadedChunks[ChunkCoord];
            return chunk[LocalCoord.X, LocalCoord.Y, coord.Z];
        }
        else
        {
            return null;
        }
    }

    public void UpdataTileMap()
    {
        if (world.player.playerData != null)
        {
            Vector2I CenterCoord = world.player.playerData.ChunkCoord;
            for (int X = CenterCoord.X - TileMapHorizon; X <= CenterCoord.X + TileMapHorizon; X++)
            {
                for (
                    int Y = CenterCoord.Y - TileMapHorizon;
                    Y <= CenterCoord.Y + TileMapHorizon;
                    Y++
                )
                {
                    Vector2I coord = new Vector2I(X, Y);
                    if (LoadedChunks.ContainsKey(coord))
                    {
                        world.VisibleChunks[coord] = LoadedChunks[coord];
                    }
                }
            }
        }


        var keysToRemove = new List<Vector2I>();
        foreach (Vector2I coord in world.VisibleChunks.Keys)
        {
            Chunk chunk = world.VisibleChunks[coord];
            Vector2I Horizon = chunk.coord - world.player.playerData.ChunkCoord;
            Horizon.X = Mathf.Abs(Horizon.X);
            Horizon.Y = Mathf.Abs(Horizon.Y);

            // 合并判断条件
            if (
                Horizon.X > TileMapHorizon
                || Horizon.Y > TileMapHorizon
                || !LoadedChunks.ContainsKey(coord)
            )
            {
                keysToRemove.Add(coord);
            }
        }

        foreach (var key in keysToRemove)
        {
            world.VisibleChunks.Remove(key, out _);
        }
    }
}