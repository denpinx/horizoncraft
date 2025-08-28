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
    public enum LightModeEnum
    {
        RayCastMode,
        DFSMode
    }

    public LightModeEnum LightMode = LightModeEnum.DFSMode;

    private int LightSize = 8;
    private int SkyLight = 8;

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

    public Vector2I LightConsuming;
    public Vector2I TileMapConsuming;
    public Stopwatch stopwatch = new Stopwatch();
    public Stopwatch stopwatch_tilemap = new Stopwatch();
    public Stopwatch stopwatch_light = new Stopwatch();

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
    public virtual bool PickItem(PlayerData playerdata, InventoryBase inventory, int index, int ActionType)
    {
        if (inventory == null) return false;
        if (playerdata == null) return false;
        var handitem = playerdata.Inventory.HandItemStack;
        inventory.update = true;
        var targetitem = inventory.GetItem(index);
        if (targetitem != null && handitem != null && targetitem.Id == handitem.Id)
        {
            //目标有物品，且id相同，且有空间
            int space = targetitem.GetItemMeta().MaxAmount - targetitem.Amount;
            if (space > 0)
            {
                if (ActionType == 1)
                {
                    targetitem.Amount += 1;
                    playerdata.Inventory.HandItemStack.Amount -= 1;
                }
                else
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
        }
        else
        {
            if (ActionType == 1)
            {
                if (handitem != null && targetitem == null)
                {
                    inventory.SetItem(index, handitem.Copy(1));
                    handitem.Amount -= 1;
                    playerdata.Inventory.update = true;
                }

                if (handitem == null && targetitem != null)
                {
                    var left = targetitem.Amount / 2;
                    var right = targetitem.Amount - left;

                    targetitem.Amount = left;
                    playerdata.Inventory.HandItemStack = targetitem.Copy(right);
                    playerdata.Inventory.update = true;
                }
            }
            else
            {
                playerdata.Inventory.update = true;
                playerdata.Inventory.HandItemStack = targetitem;
                inventory.SetItem(index, handitem);
            }
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

    public virtual void CraftGridRecipeItem(PlayerData player, bool all = false)
    {
        var gri = RecipeManage.GetRecipe(player.Inventory, 2, 36);

        if (all)
        {
            while (gri != null)
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
                else
                {
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    player.Inventory.ReduceItemAmount(36 + i);
                }

                gri = RecipeManage.GetRecipe(player.Inventory, 2, 36);
            }
        }
        else if (gri != null)
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
            else
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                player.Inventory.ReduceItemAmount(36 + i);
            }
        }
    }

    //基于光线追踪的光照计算
    public void RayCastLights(Vector3I coord, int value)
    {
        var angle_step = 16;
        float angleIncrement = 2 * Mathf.Pi / angle_step;

        for (int angle = 0; angle < angle_step; angle++)
        {
            var currentAngle = angle * angleIncrement;
            var direction = new Godot.Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
            int light = value - 1;
            for (var step = 1; step < value; step++)
            {
                if (light < 0) break;
                var offset = direction * step;
                var CurrentPos = coord + new Vector3I((int)offset.X, (int)offset.Y, 0);
                var block = GetBlock(CurrentPos);
                if (block == null) continue;
                if (block.Light < light)
                    block.Light = light;
                //else break;
                // 遇到完整方块衰减光线
                if (block.BlockMeta.CUBE) light -= 2;
                else light -= 1;
            }
        }
    }

    public void DFSUpdateLight(Vector3I coord, int value)
    {
        if (value <= 0) return;

        var block = GetBlock(coord);
        if (block == null) return;
        if (block.Light < value)
            block.Light = value;
        else
        {
            return;
        }

        if (block.BlockMeta.CUBE) value -= 2;
        else value -= 1;

        DFSUpdateLight(coord - Vector3I.Left, value);
        DFSUpdateLight(coord - Vector3I.Right, value);
        DFSUpdateLight(coord - Vector3I.Up, value);
        DFSUpdateLight(coord - Vector3I.Down, value);
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
                var gx = chunk.X * Chunk.Size + x;
                var gy = chunk.Y * Chunk.Size + y;
                var num = highmap[x, 1] - gy;
                if (num > 0) chunk.GetBlock(x, y, 1).SetLight(SkyLight);
                if (num == 0)
                {
                    if (LightMode == LightModeEnum.RayCastMode) RayCastLights(new Vector3I(gx, gy, 1), LightSize);
                    if (LightMode == LightModeEnum.DFSMode) DFSUpdateLight(new Vector3I(gx, gy, 1), LightSize);
                }
            }
        }

        if (chunk.LightList.Count > 0)
        {
            foreach (var point in chunk.LightList)
            {
                var light = new Vector2I(chunk.X * Chunk.Size + (int)point.X,
                    chunk.Y * Chunk.Size + (int)point.Y);
                if (LightMode == LightModeEnum.DFSMode)
                    DFSUpdateLight(new Vector3I((int)light.X, (int)light.Y, 1), LightSize);
                if (LightMode == LightModeEnum.RayCastMode)
                    RayCastLights(new Vector3I((int)light.X, (int)light.Y, 1), LightSize);
            }
        }
    }

    //更新区块的所有光照更新
    public void UpdateLights()
    {
        stopwatch_light.Restart();
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
            if (LightMode == LightModeEnum.DFSMode)
                DFSUpdateLight(new Vector3I(player.Coord.X, player.Coord.Y, 1), LightSize * 2);
            if (LightMode == LightModeEnum.RayCastMode)
                RayCastLights(new Vector3I(player.Coord.X, player.Coord.Y, 1), LightSize * 2);
        }

        stopwatch_light.Stop();

        LightConsuming.X = (int)stopwatch_light.ElapsedMilliseconds;
        if (LightConsuming.X > LightConsuming.Y)
            LightConsuming.Y = LightConsuming.X;
    }


    public virtual bool PlaceBlock(PlayerData player, Vector3I pos)
    {
        var pos0 = new Vector3I(pos.X, pos.Y, 0);
        var pos1 = new Vector3I(pos.X, pos.Y, 1);
        var block1 = world.WorldService.GetBlock(pos0);
        var block2 = world.WorldService.GetBlock(pos1);

        if (block1 == null || block2 == null) return false;

        Vector3I finalpos;
        Blockdata finalblock;

        if (block1.IsMeta("air"))
        {
            finalblock = block1;
            finalpos = pos0;
        }
        else
        {
            finalblock = block2;
            finalpos = pos1;
        }

        if (!finalblock.IsMeta("air")) return false;

        var item = player.Inventory.GetItem(player.Inventory.HandSlot);
        if (item == null) return false;

        BlockMeta bm = item.GetBlockMeta();
        if (bm == null) return false;

        SetBlock(finalpos, bm, false, 0);
        if (player.Mode == 0) player.Inventory.ReduceItemAmount(player.Inventory.HandSlot);
        return true;
    }

    public virtual bool BreakBlock(PlayerData player, Vector3I pos)
    {
        var fblock = world.WorldService.GetBlock(pos);
        if (world.WorldService.CheckIsCloseBlock(pos) || fblock == null || fblock.IsMeta("air")) return false;

        if (player.Mode == 0)
        {
            var item = fblock.BlockMeta?.ItemMeta?.GetItemStack();
            if (item != null)
            {
                GD.Print($"添加物品！ :{item.GetItemMeta().Name}");
                player.Inventory.TryAddItem(item);
            }
            else
            {
                GD.PrintErr($"物品不存在！ :{fblock.BlockMeta.NAME}");
            }
        }

        SetBlock(pos, Materials.Valueof("air"), false, 0);
        return true;
    }

    public virtual bool InterfaceBlock(PlayerData player, Vector3I pos)
    {
        var pos0 = new Vector3I(pos.X, pos.Y, 0);
        var pos1 = new Vector3I(pos.X, pos.Y, 1);
        var block1 = world.WorldService.GetBlock(pos0);
        var block2 = world.WorldService.GetBlock(pos1);

        if (block1 == null || block2 == null) return false;

        Vector3I finalpos;
        Blockdata InterfaceBlock;
        if (!block2.IsMeta("air"))
        {
            finalpos = pos1;
            InterfaceBlock = block2;
        }
        else
        {
            finalpos = pos0;
            InterfaceBlock = block1;
        }

        if (InterfaceBlock.IsMeta("air")) return false;

        player.OpeningBlockInventory = true;
        player.OpenInventory = new System.Numerics.Vector3(finalpos.X, finalpos.Y, finalpos.Z);
        var blockinv = InterfaceBlock.GetComponent<InventoryComponent>();
        if (blockinv == null) return false;

        world.WorldService.OpenBlockView(blockinv.InventoryName, finalpos.X, finalpos.Y, finalpos.Z);
        return true;
    }

    public virtual void CloseView()
    {
    }

    public float GetTimeProgress() => (float)(TickTimes % DayTimeMax) / DayTimeMax;
    public float GetTimeHour() => GetTimeProgress() * 24f;
}