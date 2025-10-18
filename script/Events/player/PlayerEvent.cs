using System.Collections.Generic;
using Godot;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.player;
using HorizonCraft.script.Services.world;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events.player;

/// <summary>
/// 玩家触发事件的基类
/// </summary>
public class PlayerEvent
{
    /// <summary>
    /// 世界
    /// </summary>
    public World world;

    /// <summary>
    /// 事件参与玩家
    /// </summary>
    public PlayerData Player;


    public PlayerServiceBase PlayerService => world.Service.PlayerService;
    public ChunkServiceBase ChunkService => world.Service.ChunkService;
}

/// <summary>
/// 玩家合成物品事件,该事件仅限玩家物品栏内的合成
/// </summary>
public class PlayerCraftItemEvent : PlayerEvent
{
    /// <summary>
    /// 是否全部合成
    /// </summary>
    public bool IsAllCraft = false;
}

/// <summary>
/// 玩家设置方块组件事件
/// </summary>
public class PlayerSetBlockComponentEvent : PlayerEvent
{
    public SetComponentData ComponentData;
}

/// <summary>
/// 玩家打开方块物品栏事件
/// </summary>
public class PlayerOpenBlockViewEvent : PlayerEvent
{
    /// <summary>
    /// 物品栏名
    /// </summary>
    public string ViewName;

    /// <summary>
    /// 方块位置
    /// </summary>
    public Vector3I Position;
}

/// <summary>
/// 玩家拾取物品栏物品事件
/// </summary>
public class PlayerPickItemEvent : PlayerEvent
{
    /// <summary>
    /// 目标物品栏
    /// </summary>
    public InventoryBase Inventory;

    /// <summary>
    /// 物品栏下标
    /// </summary>
    public int Index;

    /// <summary>
    /// 操作类型,0:左键,1:右键
    /// </summary>
    public int ActionType;

    public ItemStack GetIndexItem()
    {
        if (Index >= Inventory.Items.Length)
            return null;
        return Inventory.GetItem(Index);
    }

    public void SetIndexItem(ItemStack item)
        => Inventory.SetItem(Index, item);
}

/// <summary>
/// 玩家放置方块事件
/// </summary>
public class PlayerPlaceBlockEvent : PlayerEvent
{
    /// <summary>
    /// 方块坐标
    /// </summary>
    public Vector3I Position;

    /// <summary>
    /// 强制放置在指定图层
    /// </summary>
    public int CoerciveLayer = -1;

    /// <summary>
    /// 是否在背景层强制放置
    /// </summary>
    public bool CoerciveBackGroundPlace = false;

    /// <summary>
    /// 方块是否与玩家碰撞
    /// </summary>
    public bool IsCollideWithPlayer = false;

    /// <summary>
    /// 最终方块放置的结果
    /// </summary>
    public int PlaceLayerResult = 0;

    public (BlockData, Vector3I) GetBlockData()
    {
        if (CoerciveLayer != -1)
        {
            var pos = new Vector3I(Position.X, Position.Y, CoerciveLayer);
            BlockData block = ChunkService.GetBlock(pos);
            if (block == null || !block.IsMeta("air") || IsCollideWithPlayer) return (null, pos);
            return (block, pos);
        }

        var pos0 = new Vector3I(Position.X, Position.Y, 0);
        var pos1 = new Vector3I(Position.X, Position.Y, 1);
        var block1 = ChunkService.GetBlock(pos0);
        var block2 = ChunkService.GetBlock(pos1);

        if (block1 == null || block2 == null) return (null, pos0);

        Vector3I finalpos;
        BlockData finalblock;
        if (CoerciveBackGroundPlace)
        {
            finalblock = block1;
            finalpos = pos0;
        }
        else
        {
            if (block1.IsMeta("air"))
            {
                finalblock = block1;
                finalpos = pos0;
            }
            else
            {
                finalblock = block2;
                finalpos = pos1;
                if (IsCollideWithPlayer) return (null, pos0);
            }
        }

        return (finalblock, finalpos);
    }
}

/// <summary>
/// 玩家破坏方块事件
/// </summary>
public class PlayerBreakblockEvent : PlayerEvent
{
    /// <summary>
    /// 被破坏的方块坐标
    /// </summary>
    public Vector3I Position;

    /// <summary>
    /// 强制破坏的图层坐标
    /// </summary>
    public int CoerciveLayer = -1;

    public ItemStack GetItemStack()
        => Player.Inventory.GetHandItemStack();

    public BlockData GetBlockData()
    {
        if (CoerciveLayer == -1)
            return ChunkService.GetBlock(Position);
        return ChunkService.GetBlock(new Vector3I(Position.X, Position.Y, CoerciveLayer));
    }

    //public ItemStack DropItem;
    public List<ItemStack> DropLoots = new();
}

public class PlayerUseItemEvent : PlayerEvent
{
    /// <summary>
    /// 使用的物品
    /// </summary>
    public ItemStack UseItemStack;
    /// <summary>
    /// 使用物品时的目标方块
    /// </summary>
    public Vector3I Position;
}

/// <summary>
/// 玩家交互方块事件
/// </summary>
public class InterfaceBlockEvent : PlayerEvent
{
    /// <summary>
    /// 交互的方块坐标
    /// </summary>
    public Vector3I Position;
}