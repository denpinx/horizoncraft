using Horizoncraft.script.Events;

namespace Horizoncraft.script.Components.Systems.BlockSystems.Reactive;

/// <summary>
/// 门的联动更新系统，用于同步相邻门的打开状态的
/// </summary>
public class DoorLinkUpdateSystem : TickSystem
{
    private readonly BlockMeta _air = Materials.BlockMetas["air"];

    public override bool OnRightClick(PlayerRightClickBlockEvent playerRightClickBlockEvent, Component component)
    {
        var block = playerRightClickBlockEvent.blockData;
        if (block != null)
        {
            if (block.State == 0||block.State==2) block.State += 1;
            else if (block.State == 1 || block.State == 3) block.State -= 1;
            
            playerRightClickBlockEvent.Service.ChunkService.PassiveUpdateNeighborBlock(playerRightClickBlockEvent
                .Position);
            return false;
        }

        return true;
    }

    public override void ReactiveTick(BlockTickEvent blockTickEvent, ReactiveComponent component)
    {
        var block = blockTickEvent.BlockData;
        if (block.State == 0 || block.State == 1)
        {
            var topblock = blockTickEvent.GetTopBlock();
            if (topblock.BlockMeta != block.BlockMeta)
            {
                blockTickEvent.SetBlock(_air);
                blockTickEvent.UpdateNeighborBlock();
            }
            else
            {
                if (topblock.State != block.State + 2)
                {
                    block.State = topblock.State - 2;
                    blockTickEvent.UpdateNeighborBlock();
                }
            }
        }
        else if (block.State == 2 || block.State == 3)
        {
            var bottomblock = blockTickEvent.GetBottomBlock();
            if (bottomblock.BlockMeta != block.BlockMeta)
            {
                blockTickEvent.SetBlock(_air);
                blockTickEvent.UpdateNeighborBlock();
            }
            else
            {
                if (bottomblock.State != block.State - 2)
                {
                    block.State = bottomblock.State + 2;
                    blockTickEvent.UpdateNeighborBlock();
                }
            }
        }
    }
}