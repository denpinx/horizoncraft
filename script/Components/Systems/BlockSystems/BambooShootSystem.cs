using System.Linq;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems;

public class BambooShootSystem : TickSystem
{
    private readonly BlockMeta _air = Materials.BlockMetas["air"];
    private readonly BlockMeta _bamboo = Materials.BlockMetas["bamboo"];
    
    private readonly BlockMeta[] _ground =
    [
        Materials.BlockMetas["grass"],
        Materials.BlockMetas["dirt"],
    ];

    public override bool ExecuteRandBlockEvent(BlockTickEvent e, Component component)
    {
        e.SetBlock(_bamboo);
        e.UpdateNeighborBlock(true);
        return true;
    }

    public override void ReactiveTick(BlockTickEvent e, ReactiveComponent component)
    {
        var bottomBlock = e.GetBottomBlock();
        if (bottomBlock != null)
        {
            if (!_ground.Contains(bottomBlock.BlockMeta))
            {
                e.DropBlockLoot(e.BlockData);
                e.SetBlock(_air);
                e.UpdateNeighborBlock();
                return;
            }
        }
    }
}