using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.ComponentState;
using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems;

public class FluidPipeSystem : TickSystem
{
    public override void ReactiveTick(BlockTickEvent e, ReactiveComponent component)
    {
        if (component is ConnectComponent cc)
        {
            var old = cc.AngleState.Clone();
            var top = e.GetTopBlock();
            if (top != null)
            {
                if (top.BlockMeta == e.BlockData.BlockMeta)
                    cc.AngleState.Up = MathResult.Same;
                else
                {
                    var cmp = top.GetComponent<ReactiveComponent>();
                    if (cmp != null && cmp is IStorage<Fluid>)
                    {
                        cc.AngleState.Up = MathResult.Input;
                    }
                    else
                    {
                        cc.AngleState.Up = MathResult.None;
                    }
                }
            }

            var down = e.GetBottomBlock();
            if (down != null)
            {
                if (down.BlockMeta == e.BlockData.BlockMeta)
                    cc.AngleState.Down = MathResult.Same;
                else
                {
                    var cmp = down.GetComponent<ReactiveComponent>();
                    if (cmp != null && cmp is IStorage<Fluid>)
                    {
                        cc.AngleState.Down = MathResult.Input;
                    }
                    else
                    {
                        cc.AngleState.Down = MathResult.None;
                    }
                }
            }

            var left = e.GetLeftBlock();
            if (left != null)
            {
                if (left.BlockMeta == e.BlockData.BlockMeta)
                    cc.AngleState.Left = MathResult.Same;
                else
                {
                    var cmp = left.GetComponent<ReactiveComponent>();
                    if (cmp != null && cmp is IStorage<Fluid>)
                    {
                        cc.AngleState.Left = MathResult.Input;
                    }
                    else
                    {
                        cc.AngleState.Left = MathResult.None;
                    }
                }
            }

            var right = e.GetRightBlock();
            if (right != null)
            {
                if (right.BlockMeta == e.BlockData.BlockMeta)
                    cc.AngleState.Right = MathResult.Same;
                else
                {
                    var cmp = right.GetComponent<ReactiveComponent>();
                    if (cmp != null && cmp is IStorage<Fluid>)
                    {
                        cc.AngleState.Right = MathResult.Input;
                    }
                    else
                    {
                        cc.AngleState.Right = MathResult.None;
                    }
                }
            }
            
            if (!old.Same(cc.AngleState))
            {
                e.UpdateNeighborBlock();
            }
        }
    }
}