using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Components.Systems.BlockSystems.Reactive;

public class PillarSystem : TickSystem
{
    public override void ReactiveTick(BlockTickEvent e, ReactiveComponent component)
    {
        TiggerUpdate(e.BlockData, e);
    }

    public override void BlockTick(BlockTickEvent blockTickEvent, Component component)
    {
        
    }

    public void TiggerUpdate(BlockData block, BlockTickEvent e)
    {
        bool u = false, d = false;
        if (e.TryGetBlock(BlockDirecion.Down, out var down)
            && down.BlockMeta.Cube && down.BlockMeta != block.BlockMeta
           )
            d = true;

        if (e.TryGetBlock(BlockDirecion.Up, out var up)
            && up.BlockMeta.Cube && up.BlockMeta != block.BlockMeta
           )
            u = true;
        switch (u, d)
        {
            case (false, false):
                block.State = 0;
                break;
            
            case (false, true):
                block.State = 1;
                break;

            case (true, false):
                block.State = 2;
                break;

            case (true, true):

                block.State = 3;
                break;
        }
    }
}