using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class BottomCheckSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, Component component)
    {
        if (!e.CheckIsCube(e.GetBottomBlock())) e.SetBlock(Materials.Valueof("air"));
    }
}