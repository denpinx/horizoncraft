using System.Collections.Generic;
using Godot;
using horizoncraft.script.Events;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components.Systems;

public class LogisticsInputSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, Component component)
    {
        HashSet<Vector3I> finded = new HashSet<Vector3I>();

        var formblock = GetInventoryBlock(e, e.GlobalePos, true);
        if (formblock == null) return;

        var input_inv = formblock.GetComponent<InventoryComponent>();
        if (input_inv == null) return;

        var result = FindInputBlock(e, finded, e.GlobalePos);
        if (result == null) return;

        var (index, item) = input_inv.TryTakeItem(formblock.BlockMeta, 1, false);
        if (item == null) return;

        var targetmeta = result.BlockMeta;
        var target_inv = result.GetComponent<InventoryComponent>();
        if (target_inv.TryPushItem(targetmeta, item))
            input_inv.GetInventory().ReduceItemAmount(index, 1);
    }

    public BlockData FindInputBlock(BlockTickEvent e, HashSet<Vector3I> finded, Vector3I pos)
    {
        if (finded.Contains(pos) || finded.Count > 128) return null;

        var block = e.Service.ChunkService.GetBlock(pos);
        if (block == null || !block.CheckTag("link", "net")) return null;
        if (block.IsMeta("output_block"))
        {
            var invblock = GetInventoryBlock(e, pos);
            if (invblock != null) return invblock;
        }

        finded.Add(pos);
        {
            var result = FindInputBlock(e, finded, pos + Vector3I.Up);
            if (result != null) return result;
        }
        {
            var result = FindInputBlock(e, finded, pos + Vector3I.Down);
            if (result != null) return result;
        }
        {
            var result = FindInputBlock(e, finded, pos + Vector3I.Left);
            if (result != null) return result;
        }
        {
            var result = FindInputBlock(e, finded, pos + Vector3I.Right);
            if (result != null) return result;
        }
        return null;
    }

    public BlockData GetInventoryBlock(BlockTickEvent e, Vector3I pos, bool hasitem = false)
    {
        {
            var block = e.Service.ChunkService.GetBlock(pos + Vector3I.Up);
            if (block != null)
            {
                var cmp = block.GetComponent<InventoryComponent>();
                if (cmp != null)
                {
                    if (hasitem)
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
                    if (hasitem)
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
                    if (hasitem)
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
                    if (hasitem)
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