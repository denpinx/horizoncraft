using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Components.Systems;
/// <summary>
/// 物流输入系统
/// 在1tick内dfs周围的物流管道传输物品，没有缓存。
/// </summary>
public class LogisticsInputSystem : TickSystem
{
    /// <summary>
    /// 主动轮询Tick
    /// </summary>
    /// <param name="e"></param>
    /// <param name="component"></param>
    public override void BlockTick(BlockTickEvent e, Component component)
    {
        HashSet<Vector3I> passBlocks = new HashSet<Vector3I>();
        var block = GetInventoryBlock(e, e.GlobalePos, true);
        if (block == null) return;

        var inputInv = block.GetComponent<InventoryComponent>();
        if (inputInv == null) return;

        var result = FindInputBlock(e, passBlocks, e.GlobalePos);
        if (result == null) return;

        var (index, item) = inputInv.TryTakeItem(block.BlockMeta, 1, false);
        if (item == null) return;

        var targetBlockMeta = result.BlockMeta;
        var targetInv = result.GetComponent<InventoryComponent>();
        if (targetInv.TryPushItem(targetBlockMeta, item))
            inputInv.GetInventory().ReduceItemAmount(index);
    }
    /// <summary>
    /// 被动触发Tick
    /// </summary>
    /// <param name="e"></param>
    /// <param name="component"></param>
    public override void ReactiveTick(BlockTickEvent e, ReactiveComponent component)
    {
        HashSet<Vector3I> passBlocks = new HashSet<Vector3I>();
        var block = GetInventoryBlock(e, e.GlobalePos, true);
        if (block == null) return;

        var inputInv = block.GetComponent<InventoryComponent>();
        if (inputInv == null) return;

        var result = FindInputBlock(e, passBlocks, e.GlobalePos);
        if (result == null) return;

        var (index, item) = inputInv.TryTakeItem(block.BlockMeta, 1, false);
        if (item == null) return;

        var targetBlockMeta = result.BlockMeta;
        var targetInv = result.GetComponent<InventoryComponent>();
        if (targetInv.TryPushItem(targetBlockMeta, item))
            inputInv.GetInventory().ReduceItemAmount(index);
    }

    private BlockData FindInputBlock(BlockTickEvent e, HashSet<Vector3I> passBlocks, Vector3I pos)
    {
        if (passBlocks.Contains(pos) || passBlocks.Count > 128) return null;

        var block = e.Service.ChunkService.GetBlock(pos);
        if (block == null || !block.CheckTag("link", "net")) return null;
        if (block.IsMeta("output_block"))
        {
            var inventoryBlock = GetInventoryBlock(e, pos);
            if (inventoryBlock != null) return inventoryBlock;
        }

        passBlocks.Add(pos);
        {
            var result = FindInputBlock(e, passBlocks, pos + Vector3I.Up);
            if (result != null) return result;
        }
        {
            var result = FindInputBlock(e, passBlocks, pos + Vector3I.Down);
            if (result != null) return result;
        }
        {
            var result = FindInputBlock(e, passBlocks, pos + Vector3I.Left);
            if (result != null) return result;
        }
        {
            var result = FindInputBlock(e, passBlocks, pos + Vector3I.Right);
            if (result != null) return result;
        }
        return null;
    }

    private BlockData GetInventoryBlock(BlockTickEvent e, Vector3I pos, bool hasItem = false)
    {
        {
            var block = e.Service.ChunkService.GetBlock(pos + Vector3I.Up);
            if (block != null)
            {
                var cmp = block.GetComponent<InventoryComponent>();
                if (cmp != null)
                {
                    if (hasItem)
                    {
                        if (!cmp.GetInventory().IsEmpty()) return block;
                    }
                    else return block;
                }
            }
        }
        {
            var block = e.Service.ChunkService.GetBlock(pos + Vector3I.Down);
            if (block != null)
            {
                var cmp = block.GetComponent<InventoryComponent>();
                if (cmp != null)
                {
                    if (hasItem)
                    {
                        if (!cmp.GetInventory().IsEmpty()) return block;
                    }
                    else return block;
                }
            }
        }
        {
            var block = e.Service.ChunkService.GetBlock(pos + Vector3I.Left);
            if (block != null)
            {
                var cmp = block.GetComponent<InventoryComponent>();
                if (cmp != null)
                {
                    if (hasItem)
                    {
                        if (!cmp.GetInventory().IsEmpty()) return block;
                    }
                    else return block;
                }
            }
        }
        {
            var block = e.Service.ChunkService.GetBlock(pos + Vector3I.Right);
            if (block != null)
            {
                var cmp = block.GetComponent<InventoryComponent>();
                if (cmp != null)
                {
                    if (hasItem)
                    {
                        if (!cmp.GetInventory().IsEmpty()) return block;
                    }
                    else return block;
                }
            }
        }
        return null;
    }
}