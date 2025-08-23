using System;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class BlockSpreadSystem : TickSystem
{
    Random _Random = new Random();

    public override void Ticking(BlockTickEvent e, TickComponent cmp)
    {
        var ec = cmp as ExpandComponent;
        var meta = Materials.Valueof(ec.BlockName);
        if (e.CheckIsCube(e.TopBlock)) return;
        if (e.CheckMeta(e.LeftBlock, meta) && _Random.Next(0, 5) < 1)
        {
            e.SetBlockMeta = meta;
        }

        if (e.CheckMeta(e.RightBlock, meta) && _Random.Next(0, 5) < 1)
        {
            e.SetBlockMeta = meta;
        }
    }
}