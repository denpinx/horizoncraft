using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Systems;
using Horizoncraft.script.Events.player;

namespace Horizoncraft.script.Components.Systems.ItemSystems;

public class DoorLinkPlaceSystem : ItemComponentSystem
{
    private BlockMeta air = Materials.Valueof("air");

    public override bool OnPlaceBlock(PlayerPlaceBlockEvent pbe, ItemComponent itemComponent)
    {
        var down = pbe.world.Service.ChunkService.GetBlock(pbe.Position + new Vector3I(0, 1, 0));
        var top = pbe.world.Service.ChunkService.GetBlock(pbe.Position + new Vector3I(0, -1, 0));

        if (down != null && top != null)
        {
            if (!down.BlockMeta.Cube) return false;
            if (top.BlockMeta != air)
                return false;

            var meta = pbe.Player.Inventory.GetToolBarItem().GetBlockMeta();
            pbe.world.Service.ChunkService.SetBlock(pbe.Position + new Vector3I(0, -1, 0), meta,2);
            return true;
        }


        return false;
    }
}