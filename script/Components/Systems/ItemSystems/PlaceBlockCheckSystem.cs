using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Item;
using Horizoncraft.script.Events.player;

namespace HorizonCraft.script.Components.Systems.ItemSystems;

public class PlaceBlockBottomMatchSystem : ItemComponentSystem
{
    public override bool OnPlaceBlock(PlayerPlaceBlockEvent pbe, ItemComponent itemComponent)
    {
        if (itemComponent is BlockRelyOnComponent broc)
        {
            var bottomblock = pbe.world.Service.ChunkService.GetBlock(pbe.Position + new Vector3I(0, 1, 0));
            if (bottomblock == null) return false;
            if (broc.MatchTag)
            {
                if (bottomblock.CheckTag("thesaurus", broc.RelyOnBlockName))
                {
                    if (broc.State != -1)
                        return bottomblock.State == broc.State;

                    return true;
                }
            }
            else if (bottomblock.BlockMeta.Name == broc.RelyOnBlockName)
            {
                if (broc.State != -1)
                    return bottomblock.State == broc.State;

                return true;
            }
        }

        return false;
    }
}