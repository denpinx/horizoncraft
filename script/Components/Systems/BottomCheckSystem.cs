using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class BottomCheckSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, TickComponent component)
    {
        if (!e.CheckIsCube(e.BottomBlock)) e.SetBlockMeta = Materials.Valueof("air");
    }
}