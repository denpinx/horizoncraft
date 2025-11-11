using System;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems;

/// <summary>
/// 方块被蔓延系统
/// 当左右有指定的方块的时候被蔓延。
/// </summary>
public class BlockSpreadSystem : TickSystem
{
    public override bool ExecuteRandBlockEvent(BlockTickEvent e, Component component)
    {
        if (component is ExpandReactiveComponent ec)
        {
            var meta = Materials.Valueof(ec.BlockName);
            if (e.CheckIsCube(e.GetTopBlock())) return true;
            if (e.CheckMeta(e.GetLeftBlock(), meta))
            {
                e.SetBlock(meta);
                e.UpdateNeighborBlock();
            }

            if (e.CheckMeta(e.GetRightBlock(), meta))
            {
                e.SetBlock(meta);
                e.UpdateNeighborBlock();
            }
        }

        return true;
    }
}