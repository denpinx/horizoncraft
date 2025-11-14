using Godot;

namespace Horizoncraft.script.RenderSystem.render;

public class ReedRender():RenderSystem("reed_render")
{
    public override void OnDraw(RenderContext context)
    {
        var pos = context.pos_v2;
        int h = (7-context.Block.State)*2;
        Rect2 rect = new Rect2(pos * 16 + new Vector2I(0, h), new(16, 16-h));
        Rect2 src = new Rect2(new(0, h), new(16, 16 - h));
        context.Node.DrawTextureRectRegion(context.Block.BlockMeta.Texture, rect, src);
    }
}