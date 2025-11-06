using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Events.player;
using horizoncraft.script.WorldControl;

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
                BlockData result_block = null;
                Vector3I result_positon = Vector3I.Zero;
                {
                    var new_pos = new Vector3I(pos.X, pos.Y, 0);
                    var block = playerUseItemEvent.ChunkService.GetBlock(new_pos);
                    if (block != null && block.BlockMeta.Replaceable &&
                        block.BlockMeta.Name != itemFluidComponent.FluidName)
                    {
                        result_block = block;
                        result_positon = new_pos;
                    }
                }
                if (result_block == null)
                {
                    var new_pos = new Vector3I(pos.X, pos.Y, 1);
                    var block = playerUseItemEvent.ChunkService.GetBlock(new_pos);
                    if (block != null && block.BlockMeta.Replaceable &&
                        block.BlockMeta.Name != itemFluidComponent.FluidName)
                    {
                        result_block = block;
                        result_positon = new_pos;
                    }
                }
                
                if (result_block != null)
                {
                    playerUseItemEvent.ChunkService.SetBlock(result_positon, material);
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