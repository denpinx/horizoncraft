using Godot;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.RenderSystem;

public abstract class RenderSystem(string name)
{
    public string Name = name;

    public virtual void OnDraw(RenderContext context)
    {
    }
}