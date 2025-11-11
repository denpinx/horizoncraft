using System.Linq;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems;

public class ReedSystem : TickSystem
{
    private const int high_limt = 6;
    private readonly BlockMeta _air = Materials.BlockMetas["air"];
    private readonly BlockMeta _water = Materials.BlockMetas["water"];

    private readonly BlockMeta[] _ground =
    [
        Materials.BlockMetas["grass"],
        Materials.BlockMetas["dirt"],
        Materials.BlockMetas["sand"],
    ];

    private readonly Vector3I[] _positions = [new Vector3I(1, 1, 0), new Vector3I(-1, 1, 0)];

    //随机触发
    public override bool ExecuteRandBlockEvent(BlockTickEvent e, Component component)
    {
        if (component is ReactiveComponent)
        {
            var meta = e.BlockData.BlockMeta;

            if (e.BlockData.State < 7)
            {
                e.BlockData.State += 1;
                e.UpdateNeighborBlock();
                return true;
            }

            if (e.GetBottomBlock() != null && _ground.Contains(e.GetBottomBlock().BlockMeta))
            {
                if (e.CheckOneOfWithOffset(_positions, _water, out _) && e.BlockData.State == 7)
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

                        if (block.BlockMeta == meta)
                        {
                            if (block.State != 7)
                                break;
                        }
                        else
                        {
                            break;
                        }
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
                return;
            }
        }

        if (e.GetTopBlock() != null)
        {
            if (e.GetTopBlock().BlockMeta == e.BlockData.BlockMeta)
            {
                if (e.BlockData.State != 7)
                {
                    e.BlockData.State = 7;
                    e.UpdateNeighborBlock();
                }
            }
        }
    }
}