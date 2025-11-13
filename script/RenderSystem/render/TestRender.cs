using Godot;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.RenderSystem.render;

public class TestRender() : RenderSystem("test")
{
    private readonly Texture2D ice_ = GD.Load<Texture2D>("res://texture/block/iceblock.png");

    override public void OnDraw(RenderContext context)
    {
        var pos = context.Position;
        context.Node.DrawRect(new Rect2(pos * 16, new(16, 16)), Color.Color8(192, 0, 0));
    }
}