using System.Linq;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems.Reactive;

public class CactusSystem : TickSystem
{
    private readonly BlockMeta _air = Materials.BlockMetas["air"];
    private const int high_limt = 6;

    private readonly BlockMeta[] _ground =
    [
        Materials.BlockMetas["sand"],
    ];

    public override bool ExecuteRandBlockEvent(BlockTickEvent e, Component component)
    {
        if (component is ReactiveComponent)
        {
            var meta = e.BlockData.BlockMeta;
            if (e.GetBottomBlock() != null && _ground.Contains(e.GetBottomBlock().BlockMeta))
            {
                for (int i = 0; i < high_limt; i++)
                {
                    var pos = e.GlobalePos - new Vector3I(0, i, 0);
                    var block = e.World.Service.ChunkService.GetBlock(pos);
                    if (block == null) break;
                    if (block.BlockMeta == _air)
                    {
                        e.World.Service.ChunkService.SetBlock(pos, meta);
                        e.UpdateNeighborBlock();
                        break;
                    }

                    if (block.BlockMeta != meta)
                    {
                        break;
                    }
                }
            }
        }

        return true;
    }

    public override void ReactiveTick(BlockTickEvent e, ReactiveComponent component)
    {
        var bottomBlock = e.GetBottomBlock();
        if (bottomBlock != null)
        {
            if (!_ground.Contains(bottomBlock.BlockMeta) &&
                bottomBlock.BlockMeta != e.BlockData.BlockMeta)
            {
                e.DropBlockLoot(e.BlockData);
                e.SetBlock(_air);
                e.UpdateNeighborBlock();
            }
        }
    }
}