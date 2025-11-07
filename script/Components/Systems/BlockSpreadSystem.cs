using System;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;
/// <summary>
/// 方块被蔓延系统
/// 当左右有指定的方块的时候被蔓延。
/// </summary>
public class BlockSpreadSystem : TickSystem
{
    Random _Random = new Random();

    public override void BlockTick(BlockTickEvent e, Component cmp)
    {
        var ec = cmp as ExpandComponent;
        var meta = Materials.Valueof(ec.BlockName);
        if (e.CheckIsCube(e.GetTopBlock())) return;
        if (e.CheckMeta(e.GetLeftBlock(), meta) && _Random.Next(0, 5) < 1)
        {
            e.SetBlock(meta);
        }

        if (e.CheckMeta(e.GetRightBlock(), meta) && _Random.Next(0, 5) < 1)
        {
            e.SetBlock(meta);
        }
    }
}