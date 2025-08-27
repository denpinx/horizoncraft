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
using horizoncraft.script.Recipes;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.work;
using Microsoft.Data.Sqlite;
using Vector2 = System.Numerics.Vector2;

namespace HorizonCraft.script.WorldControl.Service;

public class WorldBase
{
    private int LightSize = 8;

    private Vector2I[] TerrainCoord =
    {
        new Vector2I(3, 3), //无0
        new Vector2I(3, 2), //下1
        new Vector2I(3, 0), //上2
        new Vector2I(3, 1), //上下3
        new Vector2I(2, 3), //右4
        new Vector2I(2, 2), //左上10
        new Vector2I(2, 0), //右上6
        new Vector2I(2, 1), //上下左11
        new Vector2I(0, 3), //左8
        new Vector2I(0, 2), //左下9
        new Vector2I(0, 0), //右下相同5
        new Vector2I(0, 1), //上下右7
        new Vector2I(1, 3), //左右12
        new Vector2I(1, 2), //上左右13
        new Vector2I(1, 0), //下左右
        new Vector2I(1, 1), //全部相同15
    };

    public Action<WorldBase, Chunk> OnChunkLoaded;

    public Action<WorldBase, Chunk> OnChunkUnLoading;

    //
    public WorldProfile Profile;
    public bool Connect = false;
    public bool ServerOn = false;
    public long DayTimeMax = 20 * 60;
    public long TickTimes;
    public long TickConsuming;
    public int LoadHorizon = 2;
    public int TileMapHorizon = 1;
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
            var block = chunk.GetBlock(LocalCoord.X, LocalCoord.Y, coord.Z);
            if (replaceAir)
            {
                if (block.IsMeta("air"))
                    chunk.SetBlock(LocalCoord.X, LocalCoord.Y, coord.Z, meta, state);
            }
            else
                chunk.SetBlock(LocalCoord.X, LocalCoord.Y, coord.Z, meta, state);
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

    public Vector2I GetTerrain(Vector3I pos, string tagname, string value)
    {
        var block = GetBlock(pos);
        if (block == null) return new Vector2I(1, 1);

        var up = GetBlock(pos + Vector3I.Down);
        var down = GetBlock(pos + Vector3I.Up);
        var left = GetBlock(pos + Vector3I.Left);
        var right = GetBlock(pos + Vector3I.Right);

        int state = 0;
        if (up != null && up.CheckTag(tagname, value)) state |= 1;
        if (down != null && down.CheckTag(tagname, value)) state |= 2;
        if (left != null && left.CheckTag(tagname, value)) state |= 4;
        if (right != null && right.CheckTag(tagname, value)) state |= 8;
        return TerrainCoord[state];
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

    public virtual Blockdata GetBlock(Vector3I coord, bool update = false, bool fullUpdate = false)
    {
        Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
        Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
        if (Chunks.ContainsKey(ChunkCoord))
        {
            Chunk chunk = Chunks[ChunkCoord];
            if (update)
            {
                chunk.UpdateList_buffer.Add(new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z));
            }

            return chunk.GetBlock(LocalCoord.X, LocalCoord.Y, coord.Z);
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

        var block = world.WorldService.GetBlock(new(x, y, z));
        if (block == null) return false;
        world.player.ShowView = InventoryManage.GetInventory<InventoryNode>(viewName);
        world.player.ShowView.TargetBlock = block;
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

    //修改或调用组件的自定义方法
    public virtual void SetOpenBlockComponent(PlayerData playerData, SetComponentData data)
    {
        var pos = new Vector3I((int)playerData.OpenInventory.X, (int)playerData.OpenInventory.Y,
            (int)playerData.OpenInventory.Z);
        var block = GetBlock(pos);
        if (block != null)
            ComponentManager.SetBlockComponentData(playerData, block, data);
    }

    public virtual void CraftGridRecipeItem(PlayerData player)
    {
        var gri = RecipeManage.GetRecipe(player.Inventory, 2, 36);
        if (gri != null)
        {
            var handitme = player.Inventory.HandItemStack;
            if (handitme == null
               )
            {
                player.Inventory.HandItemStack = gri.Result.Copy();
            }
            else if (
                handitme.Id == gri.Result.Id &&
                handitme.Amount + gri.Result.Amount <= gri.Result.GetItemMeta().MaxAmount
            )
            {
                handitme.Amount += gri.Result.Amount;
            }

            for (int i = 0; i < 4; i++)
            {
                player.Inventory.ReduceItemAmount(36 + i);
            }
        }
    }


    public void ManhattanRange(int cx, int cy, int r)
    {
        int d = 2 * r + 1; // 菱形外接正方形边长
        int total = d * d; // 总格子数

        for (int k = 0; k < total; k++)
        {
            int ox = k % d - r; // [-r , r]
            int oy = k / d - r; // [-r , r]
            int tr = Math.Abs(ox) + Math.Abs(oy);
            if (tr <= r) // 曼哈顿距离过滤
            {
            }
        }
    }


    //更新光源
    public void UpdateLightPoint(int x, int y, int value)
    {
        int d = 2 * value + 1; // 菱形外接正方形边长
        int total = d * d; // 总格子数
        for (int k = 0; k < total; k++)
        {
            int ox = k % d - value; // [-r , r]
            int oy = k / d - value; // [-r , r]
            int tr = Math.Abs(ox) + Math.Abs(oy);
            if (tr <= value) // 曼哈顿距离过滤
            {
                SetBlockLight(new Vector3I(x + ox, y + oy, 1), value - tr);
            }
        }
    }

    public void SetBlockLight(Vector3I coord, int value)
    {
        var block = GetBlock(coord);
        if (block == null) return;

        if (block.Light < value)
            block.Light = value;
    }

    //更新单个区块的所有光源
    public void UpdataChunkLight(Chunk chunk)
    {
        var highmap = chunk.HighMap;
        if (highmap == null)
            chunk.HighMap = WorldGenerator.GetHighMap(chunk.X);
        
        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                var gy = chunk.Y * Chunk.Size + y;
                int skyLight = LightSize + Math.Min(0, highmap[x, 1] - gy);
                skyLight = Math.Clamp(skyLight, 0, LightSize);

                chunk.GetBlock(x, y, 1).SetLight(skyLight);
            }
        }

        if (chunk.LightList.Count > 0)
        {
            foreach (var point in chunk.LightList)
            {
                var light = new Vector2I(chunk.X * Chunk.Size + (int)point.X,
                    chunk.Y * Chunk.Size + (int)point.Y);
                UpdateLightPoint((int)light.X, (int)light.Y, LightSize);
            }
        }
    }

    //更新区块的所有光照更新

    public void UpdateLights()
    {
        foreach (var sts in Chunks)
            sts.Value.ClearLight();

        foreach (var sts in Chunks)
        {
            var chunk = sts.Value;
            UpdataChunkLight(chunk);
        }

        foreach (var sts in Players)
        {
            var player = sts.Value;
            UpdateLightPoint(player.Coord.X, player.Coord.Y, LightSize * 2);
        }
    }

    public virtual void CloseView()
    {
    }

    public float GetTimeProgress() => (float)(TickTimes % DayTimeMax) / DayTimeMax;
    public float GetTimeHour() => GetTimeProgress() * 24f;
}