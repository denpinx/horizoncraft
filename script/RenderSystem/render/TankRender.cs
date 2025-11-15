using System.Linq;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.ComponentState;

namespace Horizoncraft.script.RenderSystem.render;

public class TankRender() : RenderSystem("tank_render")
{
    public override void OnDraw(RenderContext context)
    {
        var cmp = context.Block.GetComponent<ReactiveComponent>();
        if (cmp != null && cmp is IStorage<Fluid> getter)
        {
            var fluid = getter.GetStorage().States.First().Value;
            if (fluid.Amount > 0)
            {
                var texture = Materials.BlockMetas[fluid.Name].Texture;
                var pos = context.pos_v2;
                int h = 16 - (context.Block.State * 2);
                Rect2 rect = new Rect2(pos * 16 + new Vector2I(0, h), new(16, 16 - h));
                Rect2 src = new Rect2(new(0, h), new(16, 16 - h));
                context.Node.DrawTextureRectRegion(texture, rect, src);
            }
        }
    }
}