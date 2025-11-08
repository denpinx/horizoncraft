using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Item;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Components.Systems.ItemSystems;

public class PlaceFluidSystem : ItemComponentSystem
{
    public override bool OnItemUse(PlayerUseItemEvent playerUseItemEvent, ItemComponent itemComponent)
    {
        if (itemComponent is ItemFluidComponent itemFluidComponent)
        {
            if (Materials.BlockMetas.TryGetValue(itemFluidComponent.FluidName, out var material))
            {
                var pos = playerUseItemEvent.Position;
                BlockData resultBlock = null;
                Vector3I resultPositon = Vector3I.Zero;
                {
                    var newPos = new Vector3I(pos.X, pos.Y, 0);
                    var block = playerUseItemEvent.ChunkService.GetBlock(newPos);
                    if (block != null && block.BlockMeta.Replaceable &&
                        block.BlockMeta.Name != itemFluidComponent.FluidName)
                    {
                        resultBlock = block;
                        resultPositon = newPos;
                    }
                }
                if (resultBlock == null)
                {
                    var newPos = new Vector3I(pos.X, pos.Y, 1);
                    var block = playerUseItemEvent.ChunkService.GetBlock(newPos);
                    if (block != null && block.BlockMeta.Replaceable &&
                        block.BlockMeta.Name != itemFluidComponent.FluidName)
                    {
                        resultBlock = block;
                        resultPositon = newPos;
                    }
                }
                
                if (resultBlock != null)
                {
                    playerUseItemEvent.ChunkService.SetBlock(resultPositon, material);
                    playerUseItemEvent.UseItemStack.Amount -= 1;
                }
            }
            else
            {
                GD.PrintErr($"{nameof(PlaceFluidSystem)}: 方块{itemFluidComponent.FluidName} 不存在");
            }
        }

        return true;
    }
}