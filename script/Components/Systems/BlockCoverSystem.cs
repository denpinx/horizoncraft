using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class BlockCoverSystem : TickSystem
{
    public override void Ticking(BlockTickEvent e, TickComponent cmp)
    {
        var ec = cmp as ExpandComponent;
        if (e.CheckIsCube(e.TopBlock)) e.Blockdata.SetMeta(Materials.Valueof(ec.BlockName));
    }
}