using System;
using Godot;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.RenderSystem.render;

public class WaterRender() : RenderSystem("water_render")
{
    private Texture2D texture = GD.Load<Texture2D>("res://texture/block/water.png");

    public override void OnDraw(RenderContext context)
    {
        var pos = context.pos_v2;
        int h = context.Block.State;
        Rect2 rect = new Rect2(pos * 16 + new Vector2I(0, h), new(16, 16-h));
        Rect2 src = new Rect2(new(0, h), new(16, 16 - h));
        context.Node.DrawTextureRectRegion(context.Block.BlockMeta.Texture, rect, src);
    }
}