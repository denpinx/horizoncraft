using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Features;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.work;
using Microsoft.Data.Sqlite;

namespace HorizonCraft.script.WorldControl.Service;

public class WorldBase
{
    public Action<WorldBase, Chunk> OnChunkLoaded;

    public Action<WorldBase, Chunk> OnChunkUnLoading;

    //
    public WorldProfile Profile;
    public bool Connect = false;
    public bool ServerOn = false;
    public long DayTimeMax = 20 * 60;
    public long TickTimes;
    public long TickConsuming;
    public int LoadHorizon = 3;
    public int TileMapHorizon = 2;
    public Stopwatch stopwatch = new Stopwatch();

    /// <summary>
    /// 已加载区块
    /// </summary>
    public ConcurrentDictionary<Vector2I, Chunk> Chunks = new();

    /// <summary>
    /// 已加载玩家
    /// </summary>
    public ConcurrentDictionary<string, PlayerData> Players = new();

    /// <summary>
    /// 待卸载区块
    /// </summary>
    public ConcurrentDictionary<Vector2I, Chunk> OffloadChunkQueue = new();

    /// <summary>
    /// 待加载玩家
    /// </summary>
    public ConcurrentQueue<string> LoadingPlayers = new();

    /// <summary>
    /// 待加载区块
    /// </summary>
    public ConcurrentDictionary<Vector2I, WorkBase> LoadChunkQueue = new();

    public Dictionary<Vector2I, int> UnloadCount = new();

    public bool Lock = false;
    public World world;

    public WorldBase()
    {
    }

    public virtual void SetBlock(Vector3I coord, Blockdata blockdata)
    {
        Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
        Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
        if (Chunks.ContainsKey(ChunkCoord))
        {
            Chunk chunk = Chunks[ChunkCoord];
            chunk.SetBlock(LocalCoord.X, LocalCoord.Y, coord.Z, blockdata);
        }
    }

    public virtual void SetBlock(Vector3I coord, BlockMeta meta, bool replaceAir = false, int state = 0)
    {
        Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
        Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
        if (Chunks.ContainsKey(ChunkCoord))
        {
            Chunk chunk = Chunks[ChunkCoord];
            if (replaceAir)
            {
                if (chunk[LocalCoord.X, LocalCoord.Y, coord.Z].IsMeta("air"))
                {
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z].SetMeta(meta);
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                    chunk.UpdateList_buffer.Add(new(LocalCoord.X,
                        LocalCoord.Y,
                        coord.Z));
                    chunk.update_tilemap = true;
                }
            }
            else
            {
                chunk[LocalCoord.X, LocalCoord.Y, coord.Z].SetMeta(meta);
                chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                chunk.UpdateList_buffer.Add(new(LocalCoord.X,
                    LocalCoord.Y,
                    coord.Z));
                chunk.update_tilemap = true;
            }
        }
        else
        {
            if (LoadChunkQueue.ContainsKey(ChunkCoord))
            {
                if (LoadChunkQueue[ChunkCoord].Type == "NONE")
                {
                    SetBlockWork sbw = new SetBlockWork()
                    {
                        Type = "SETBLOCK",
                        ExclList = new List<(Vector3I, BlockMeta, int)>(),
                    };
                    sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                    LoadChunkQueue[ChunkCoord] = sbw;
                }
                else
                {
                    SetBlockWork stw = (SetBlockWork)LoadChunkQueue[ChunkCoord];
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
                LoadChunkQueue[ChunkCoord] = sbw;
            }
        }
    }

    public bool CheckIsCloseBlock(Vector3I pos)
    {
        var block = GetBlock(pos);
        var u = GetBlock(pos + Vector3I.Down);
        var d = GetBlock(pos + Vector3I.Up);
        var l = GetBlock(pos + Vector3I.Left);
        var r = GetBlock(pos + Vector3I.Right);
        if (block != null && u != null && d != null && l != null && r != null)
        {
            if (
                !u.IsMeta("air") &&
                !d.IsMeta("air") &&
                !l.IsMeta("air") &&
                !r.IsMeta("air")
            )
            {
                return true;
            }
        }

        return false;
    }

    public virtual Blockdata GetBlock(Vector3I coord)
    {
        Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
        Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
        if (Chunks.ContainsKey(ChunkCoord))
        {
            Chunk chunk = Chunks[ChunkCoord];
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
                    if (Chunks.ContainsKey(coord))
                    {
                        world.VisibleChunks[coord] = Chunks[coord];
                        UnloadCount.Remove(coord);
                    }
                    else
                    {
                        if (UnloadCount.ContainsKey(coord))
                            UnloadCount[coord] += 1;
                        else UnloadCount[coord] = 1;

                        if (UnloadCount[coord] == 40)
                        {
                            if (Connect)
                            {
                                world.RpcId(1, "UpdateChunk", X, Y);
                            }
                        }
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
                || !Chunks.ContainsKey(coord)
            )
            {
                UnloadCount.Remove(coord);
                keysToRemove.Add(coord);
            }
        }

        foreach (var key in keysToRemove)
        {
            world.VisibleChunks.Remove(key, out _);
        }
    }

    public virtual void SharItem(InventoryBase inventory, int index)
    {
    }
    //不直接写在 InventoryBase 因为这涉及到用户交互和网络传输
    public virtual bool PickItem(PlayerData playerdata, InventoryBase inventory, int index)
    {
        if (playerdata == null) return false;
        var handitem = playerdata.Inventory.HandItemStack;
        var targetitem = inventory.GetItem(index);
        if (targetitem != null && handitem != null && targetitem.Id == handitem.Id)
        {
            int space = targetitem.GetItemMeta().MaxAmount - targetitem.Amount;
            if (space > 0)
            {
                if (space >= handitem.Amount)
                {
                    targetitem.Amount += handitem.Amount;
                    playerdata.Inventory.HandItemStack = null;
                }
                else
                {
                    targetitem.Amount += space;
                    handitem.Amount -= space;
                }
            }
        }
        else
        {
            playerdata.Inventory.update = true;
            playerdata.Inventory.HandItemStack = targetitem;
            inventory.SetItem(index, handitem);
        }

        return true;
    }

    public bool OpenBlockView(string viewName, int x, int y, int z)
    {
        if (world == null || world.player.playerData == null) return false;
        if (world.player.ShowView != null)
        {
            CloseView();
            world.player.RemoveChild(world.player.ShowView);
        }

        var Invcomponent = world.WorldService.GetBlock(new(x, y, z))?.GetComponent<InventoryComponent>();
        if (Invcomponent == null) return false;
        world.player.ShowView = InventoryManage.GetInventory<InventoryNode>(viewName);
        world.player.ShowView.TargetInvBase = Invcomponent.GetInventory();
        world.player.ShowView.TargetBlockGlobalPos = new(x, y, z);
        world.player.ShowView.player = world.player;
        world.player.AddChild(world.player.ShowView);
        return true;
    }

    public void OpenView(string viewName)
    {
        if (world.player.ShowView != null)
        {
            CloseView();
            world.player.RemoveChild(world.player.ShowView);
        }

        world.player.ShowView = InventoryManage.GetInventory<InventoryNode>(viewName);
        world.player.ShowView.player = world.player;
        world.player.AddChild(world.player.ShowView);
    }

    public virtual void CloseView()
    {
    }

    public float GetTimeProgress() => (float)(TickTimes % DayTimeMax) / DayTimeMax;
    public float GetTimeHour() => GetTimeProgress() * 24f;
}