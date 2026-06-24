using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems;

/// <summary>
/// 流体系统
/// 当被手持铁桶右键时会被替换成空气，并基给予玩家一个水桶。
/// 每Tick会向周围蔓延，直到最大限制。
/// </summary>
public class FluidSystem : TickSystem
{
    const int FluidLenght = 16;
    BlockMeta air = Materials.Valueof("air");

    public override bool OnRightClick(PlayerRightClickBlockEvent playerRightClickBlockEvent, Component component)
    {
        var inventory = playerRightClickBlockEvent.Player.Inventory;
        var item = inventory.GetToolBarItem();
        if (item == null) return true;
        if (item.GetItemMeta().Name == "iron_bucket")
        {
            if (Materials.ItemMetas.TryGetValue($"iron_bucket_{playerRightClickBlockEvent.blockData.BlockMeta.Name}",
                    out var meta))
            {
                if (inventory.TryAddItem(meta.GetItemStack()))
                {
                    item.Amount -= 1;
                    playerRightClickBlockEvent.Player.Inventory.Update = true;
                    playerRightClickBlockEvent.Service.ChunkService.SetBlock(playerRightClickBlockEvent.Position, air);
                }
            }
        }

        return true;
    }

    public override void BlockTick(BlockTickEvent e, Component cmp)
    {
        BlockComponents.FluidComponent fc = cmp as BlockComponents.FluidComponent;
        BlockMeta blockMeta = Materials.Valueof(fc.BlockName);
        //四周蔓延衰减
        if (e.CheckCanReplaceAndNotMeta(e.GetBottomBlock(), blockMeta))
        {
            e.DropBlockLoot(e.GetBottomBlock());
            e.SetBottomBlock(blockMeta, 0);
            
            if(e.GetBottomBlock().TryGetComponent<BlockComponents.FluidComponent>("FluidComponent", out var fluidComponent))
                fluidComponent.mobility = true;            
            return;
        }

        if (e.GetBottomBlock() != null)
        {
            if (e.CheckMeta(e.GetBottomBlock(), blockMeta))
            {
                e.GetBottomBlock().State = 0;
            }
            else
            {
                if (e.CheckCanReplaceAndNotMeta(e.GetLeftBlock(), blockMeta) && e.BlockData.State < FluidLenght - 1)
                {
                    e.DropBlockLoot(e.GetLeftBlock());
                    e.SetLeftBlock(blockMeta, e.BlockData.State + 1);

                    if(e.GetLeftBlock().TryGetComponent<BlockComponents.FluidComponent>("FluidComponent", out var fluidComponent))
                        fluidComponent.mobility = true;    
                    
                    
                    return;
                }

                if (e.CheckCanReplaceAndNotMeta(e.GetRightBlock(), blockMeta) && e.BlockData.State < FluidLenght - 1)
                {
                    e.DropBlockLoot(e.GetRightBlock());
                    e.SetRightBlock(blockMeta, e.BlockData.State + 1);
                    if(e.GetRightBlock().TryGetComponent<BlockComponents.FluidComponent>("FluidComponent", out var fluidComponent))
                        fluidComponent.mobility = true;    

                    return;
                }
            }
        }


        //顶部没有方块
        if (e.GetTopBlock() != null && !e.CheckMeta(e.GetTopBlock(), blockMeta))
        {
            if (fc.mobility)
            {
                if (e.BlockData.State == 0)
                {
                    e.SetBlock(air);
                    return;
                }
                else
                {
                    if ((!e.CheckMeta(e.GetLeftBlock(), blockMeta) || (e.CheckMeta(e.GetLeftBlock(), blockMeta) &&
                                                                       e.GetLeftBlock().State > e.BlockData.State &&
                                                                       e.GetLeftBlock().State > 0)) &&
                        (!e.CheckMeta(e.GetRightBlock(), blockMeta) || (e.CheckMeta(e.GetRightBlock(), blockMeta) &&
                                                                        e.GetRightBlock().State > e.BlockData.State &
                                                                        e.GetRightBlock().State > 0))
                       )
                    {
                        e.SetBlock(air);
                        return;
                    }


                    if (e.GetLeftBlock() != null && e.CheckMeta(e.GetLeftBlock(), blockMeta) &&
                        e.GetLeftBlock().State < e.BlockData.State - 2 &&
                        e.BlockData.State <= FluidLenght - 1)
                    {
                        e.BlockData.State = e.GetLeftBlock().State + 1;
                    }

                    if (e.GetRightBlock() != null && e.CheckMeta(e.GetRightBlock(), blockMeta) &&
                        e.GetRightBlock().State < e.BlockData.State - 2 &&
                        e.BlockData.State <= FluidLenght - 1)
                    {
                        e.BlockData.State = e.GetRightBlock().State + 1;
                    }
                }
            }
        }
    }
}