using System;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Item;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Recipes;
using Horizoncraft.script.Services.world;
using Horizoncraft.script.WorldControl;
using InventoryComponent = Horizoncraft.script.Components.BlockComponents.InventoryComponent;
using Vector2 = System.Numerics.Vector2;

namespace Horizoncraft.script.Events;

/// <summary>
/// 玩家事件处理集
/// </summary>
public class PlayerEvents
{
    /// <summary>
    /// 设置玩家当前打开方块的组件信息
    /// </summary>
    /// <param name="e"></param>
    public virtual void SetOpenBlockComponent(PlayerSetBlockComponentEvent e)
    {
        if (e.ChunkService.TryGetBlock(e.Player.OpenInventory.ToVector3I(), out var block))
            ComponentManager.SetBlockComponentData(e.Player, block, e.ComponentData);
    }

    /// <summary>
    /// 合成玩家配方
    /// </summary>
    /// <param name="playerCraftItemEvent"></param>
    public virtual void CraftGridRecipeItem(PlayerCraftItemEvent playerCraftItemEvent)
    {
        var gri = RecipeManage.GetRecipe(playerCraftItemEvent.Player.Inventory, 2, 36);
        if (playerCraftItemEvent.IsAllCraft)
        {
            //一键合成,直接返回背包
            while (gri != null)
            {
                if (!playerCraftItemEvent.Player.Inventory.TryAddItem(gri.Result.Copy()))
                    return;
                for (int i = 0; i < 4; i++)
                    playerCraftItemEvent.Player.Inventory.ReduceItemAmount(36 + i);
                gri = RecipeManage.GetRecipe(playerCraftItemEvent.Player.Inventory, 2, 36);
            }
        }
        else if (gri != null)
        {
            //手动合成
            var handitme = playerCraftItemEvent.Player.Inventory.GetHandItemStack();
            if (handitme == null
               )
            {
                playerCraftItemEvent.Player.Inventory.HandItemStack = gri.Result.Copy();
            }
            else if (
                handitme.Name == gri.Result.Name &&
                handitme.Amount + gri.Result.Amount <= gri.Result.GetItemMeta().MaxAmount
            )
                handitme.Amount += gri.Result.Amount;
            else return;


            for (int i = 0; i < 4; i++)
                playerCraftItemEvent.Player.Inventory.ReduceItemAmount(36 + i);
        }
    }

    /// <summary>
    /// 让玩家打开物品栏
    /// </summary>
    /// <param name="world">世界</param>
    /// <param name="viewName">物品栏名</param>
    public void OpenInventory(World world, string viewName)
    {
        if (world.PlayerNode.OpeningInventoryNode != null)
        {
            world.PlayerNode.RemoveChild(world.PlayerNode.OpeningInventoryNode);
        }

        world.PlayerNode.OpeningInventoryNode = InventoryManage.GetInventory<InventoryNode>(viewName);
        world.PlayerNode.OpeningInventoryNode.PlayerNode = world.PlayerNode;
        world.PlayerNode.AddChild(world.PlayerNode.OpeningInventoryNode);
    }

    /// <summary>
    /// 接收查看的方块信息
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public virtual bool ReciveLookingBlockData(PlayerOpenBlockViewEvent e)
    {
        //TODO 待移除
        // var world = e.world;
        // if (world.PlayerNode.ShowView != null)
        // {
        //     world.PlayerNode.RemoveChild(world.PlayerNode.ShowView);
        // }
        //
        // var block = world.Service.ChunkService.GetBlock(e.Position);
        // if (block == null) return false;
        // world.PlayerNode.ShowView = InventoryManage.GetInventory<InventoryNode>(e.ViewName);
        // world.PlayerNode.ShowView.TargetBlock = block;
        // world.PlayerNode.ShowView.PlayerNode = world.PlayerNode;
        // world.PlayerNode.AddChild(world.PlayerNode.ShowView);
        return true;
    }

    /// <summary>
    /// 打开方块的物品栏
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public virtual bool OpenBlockView(PlayerOpenBlockViewEvent e)
    {
        var world = e.world;
        if (world.PlayerNode.OpeningInventoryNode != null)
        {
            world.PlayerNode.RemoveChild(world.PlayerNode.OpeningInventoryNode);
            world.PlayerNode.OpeningInventoryNode = null;
        }

        var block = world.Service.ChunkService.GetBlock(e.Position);
        if (block == null) return false;
        world.PlayerNode.OpeningInventoryNode = InventoryManage.GetInventory<InventoryNode>(e.ViewName);
        world.PlayerNode.OpeningInventoryNode.TargetBlock = block;
        world.PlayerNode.OpeningInventoryNode.PlayerNode = world.PlayerNode;
        world.PlayerNode.AddChild(world.PlayerNode.OpeningInventoryNode);
        return true;
    }

    /// <summary>
    /// 拾取物品到手中
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public virtual bool PickItem(PlayerPickItemEvent e)
    {
        if (e.Inventory == null) return false;
        if (e.Player == null) return false;
        var handitem = e.Player.Inventory.GetHandItemStack();
        e.Inventory.update = true;
        var targetitem = e.GetIndexItem();
        if (targetitem != null && handitem != null && targetitem.Name == handitem.Name)
        {
            //目标有物品，且id相同，且有空间
            int space = targetitem.GetItemMeta().MaxAmount - targetitem.Amount;
            if (space > 0)
            {
                if (e.ActionType == 1)
                {
                    targetitem.Amount += 1;
                    e.Player.Inventory.HandItemStack.Amount -= 1;
                }
                else
                {
                    if (space >= handitem.Amount)
                    {
                        targetitem.Amount += handitem.Amount;
                        e.Player.Inventory.HandItemStack = null;
                    }
                    else
                    {
                        targetitem.Amount += space;
                        handitem.Amount -= space;
                    }
                }
            }
        }
        else
        {
            if (e.ActionType == 1)
            {
                if (handitem != null && targetitem == null)
                {
                    e.SetIndexItem(handitem.Copy(1));
                    handitem.Amount -= 1;
                    e.Player.Inventory.update = true;
                }

                if (handitem == null && targetitem != null)
                {
                    var left = targetitem.Amount / 2;
                    var right = targetitem.Amount - left;

                    targetitem.Amount = left;
                    e.Player.Inventory.HandItemStack = targetitem.Copy(right);
                    e.Player.Inventory.update = true;
                }
            }
            else
            {
                e.Player.Inventory.update = true;
                e.Player.Inventory.HandItemStack = targetitem;
                e.Inventory.SetItem(e.Index, handitem);
            }
        }

        return true;
    }

    /// <summary>
    /// 放置方块
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public virtual bool PlaceBlock(PlayerPlaceBlockEvent e)
    {
        var set = e.GetBlockData();
        var block = set.Item1;
        var pos = set.Item2;
        e.PlaceLayerResult = pos.Z;
        if (block == null) return false;
        if (!block.IsMeta("air")) return false;

        var item = e.Player.Inventory.GetItem(e.Player.Inventory.ToolBarIndex);
        if (item == null) return false;

        BlockMeta bm = item.GetBlockMeta();
        if (bm == null) return false;

        e.Position = pos;

        if (item.Components.Count > 0)
            if (!ComponentManager.ExecuteItemComponents(e, item))
                return false;

        e.ChunkService.SetBlock(pos, bm);
        e.ChunkService.PassiveUpdateNeighborBlock(pos, true);

        if (e.Player.Mode == 0)
        {
            e.Player.Inventory.ReduceItemAmount(e.Player.Inventory.ToolBarIndex);
        }

        return true;
    }

    /// <summary>
    /// 破坏方块
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public virtual bool BreakBlock(PlayerBreakblockEvent e)
    {
        if (e.Player.State != PlayerState.Live) return false;

        var targetblock = e.GetBlockData();

        if (e.Player.Mode == 0 &&
            targetblock != null &&
            !targetblock.IsMeta("air") &&
            !e.ChunkService.CheckIsCloseBlock(e.Position))
        {
            var item = targetblock.BlockMeta?.ItemMeta?.GetItemStack();
            if (item != null)
            {
                var handItem = e.Player.Inventory.GetItem(e.Player.Inventory.ToolBarIndex);
                //执行物品组件事件,由事件决定掉落物
                if (handItem != null && handItem.Components.Count > 0)
                    ComponentManager.ExecuteItemComponents(e, handItem);
                //没有物品组件则默认掉落方块的直接物品
                else
                    e.DropLoots = e.GetBlockData().BlockMeta.LootTable.TryTakeItem(e.GetBlockData().State);
            }

            foreach (var dropitem in e.DropLoots)
            {
                var data = dropitem.GetEntityData(new Vector2I(e.Position.X * 16, e.Position.Y * 16));
                e.world.Service.EntityService.AddEntityData(data);
            }

            if (Random.Shared.Next(10) == 0 && e.Player.Hunger.Value > 0)
                e.Player.Hunger.Value -= 0.1f;

            targetblock.DropBlockInventoryItems(e.world, new Vector2I(e.Position.X, e.Position.Y));
        }


        e.ChunkService.SetBlock(e.Position, Materials.Valueof("air"));
        e.ChunkService.PassiveUpdateNeighborBlock(e.Position);
        e.Player.Inventory.update = true;
        return true;
    }

    /// <summary>
    /// 右键交互方块
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public virtual bool InterfaceBlock(InterfaceBlockEvent e)
    {
        var pos0 = new Vector3I(e.Position.X, e.Position.Y, 0);
        var pos1 = new Vector3I(e.Position.X, e.Position.Y, 1);
        var backblock = e.ChunkService.GetBlock(pos0);
        var fontblock = e.ChunkService.GetBlock(pos1);

        if (backblock == null || fontblock == null) return false;
        Vector3I finalpos;
        BlockData InterfaceBlock;
        if (!fontblock.IsMeta("air"))
        {
            finalpos = pos1;
            InterfaceBlock = fontblock;
        }
        else
        {
            finalpos = pos0;
            InterfaceBlock = backblock;
        }

        if (InterfaceBlock.IsMeta("air")) return false;

        e.Player.OpeningBlockInventory = true;
        e.Player.OpenInventory = new System.Numerics.Vector3(finalpos.X, finalpos.Y, finalpos.Z);
        var blockinv = InterfaceBlock.GetComponent<InventoryComponent>();
        //if (blockinv == null)
        //{

        PlayerRightClickBlockEvent prcbe = new PlayerRightClickBlockEvent()
        {
            World = e.world,
            Service = e.world.Service,
            Player = e.Player,
            Position = finalpos,
            blockData = InterfaceBlock
        };
        var state = InterfaceBlock.State;
        var result = ComponentManager.ExecuteBlockComponents(prcbe, InterfaceBlock);
        if (InterfaceBlock.State != state)
        {
            var pos = finalpos.MathFloor(Chunk.Size);
            if (e.world.Service.ChunkService.Chunks.TryGetValue(pos, out var chunk))
            {
                var local_pos = finalpos.Remainder(Chunk.Size);
                chunk.update_tilemap = true;
                chunk.UpdateList_buffer.Add(new Vector3I(local_pos.X, local_pos.Y, finalpos.Z));
            }
        }

        if (!result)
        {
            return result;
        }


        if (blockinv == null) return false;

        var pobve = new PlayerOpenBlockViewEvent()
        {
            Player = e.Player,
            world = e.world,
            Position = finalpos,
            ViewName = blockinv.InventoryName
        };
        e.PlayerService.Events.OpenBlockView(pobve);
        return true;
    }

    public virtual bool UseItem(PlayerUseItemEvent e)
    {
        var cmp = e.UseItemStack.GetComponent<ItemUsefulComponent>();

        return ComponentManager.ExecuteItemComponents<ItemUsefulComponent>(e, e.UseItemStack);
    }


    /// <summary>
    /// 关闭物品栏订阅
    /// </summary>
    /// <param name="service"></param>
    /// <param name="name"></param>
    public virtual void CloseInventory(WorldServiceBase service, string name)
    {
        if (service.PlayerService.Players.TryGetValue(name, out var player))
        {
            player.OpeningBlockInventory = false;
        }
    }

    /// <summary>
    /// 丢弃一个物品
    /// </summary>
    /// <param name="service"></param>
    /// <param name="name"></param>
    public virtual void DropItem(WorldServiceBase service, string name)
    {
        if (service.PlayerService.Players.TryGetValue(name, out var player))
        {
            var item = player.Inventory.GetToolBarItem();
            if (item != null && item.Amount > 0)
            {
                var pos = player.Position;
                if (player.FaceLeft) pos -= Vector2.UnitX * 16 + Vector2.UnitY * 32;
                else pos -= Vector2.UnitX * -16 + Vector2.UnitY * 32;
                var data = item.Copy(1).GetEntityData(pos.ToVector2I());
                item.Amount -= 1;
                data.Update = true;
                service.EntityService.AddEntityData(data);
                player.Inventory.update = true;
            }
        }
    }

    /// <summary>
    /// 丢弃一组物品
    /// </summary>
    /// <param name="service"></param>
    /// <param name="name"></param>
    public virtual void DropAllItem(WorldServiceBase service, string name)
    {
        if (service.PlayerService.Players.TryGetValue(name, out var player))
        {
            var item = player.Inventory.GetToolBarItem();
            if (item != null && item.Amount > 0)
            {
                var pos = player.Position;
                if (player.FaceLeft) pos -= Vector2.UnitX * 16 + Vector2.UnitY * 32;
                else pos -= Vector2.UnitX * -16 + Vector2.UnitY * 32;
                var data = item.GetEntityData(pos.ToVector2I());
                data.Update = true;
                player.Inventory.SetItem(player.Inventory.ToolBarIndex, null);
                service.EntityService.AddEntityData(data);
                player.Inventory.update = true;
            }
        }
    }

    /// <summary>
    /// 快速转移物品
    /// </summary>
    /// <param name="service"></param>
    /// <param name="player_name"></param>
    /// <param name="index"></param>
    public virtual void QuickClickBlockInvItem(WorldServiceBase service, string player_name, int index)
    {
        if (service.PlayerService.Players.TryGetValue(player_name, out var player))
        {
            if (!player.OpeningBlockInventory) return;
            var block = service.ChunkService.GetBlock(player.OpenInventory.ToVector3I());
            var cmp = block.GetComponent<InventoryComponent>();
            if (cmp == null) return;
            var item = cmp.GetInventory().GetItem(index);
            if (item == null) return;
            if (player.Inventory.HasSpace(item))
            {
                cmp.GetInventory().SetItem(index, null);
                player.Inventory.TryAddItem(item);
            }
        }
    }

    /// <summary>
    /// 快速转移物品
    /// </summary>
    /// <param name="service"></param>
    /// <param name="player_name"></param>
    /// <param name="index"></param>
    public virtual void QuickClickPlayerInvItem(WorldServiceBase service, string player_name, int index)
    {
        if (service.PlayerService.Players.TryGetValue(player_name, out var player))
        {
            GD.Print($"{player_name}, {index}");
            var item = player.Inventory.GetItem(index);
            if (item == null) return;

            if (player.OpeningBlockInventory)
            {
                var block = service.ChunkService.GetBlock(player.OpenInventory.ToVector3I());
                var cmp = block.GetComponent<InventoryComponent>();
                if (cmp == null) return;
                var block_inv = cmp.GetInventory();
                if (block_inv.HasSpace(item))
                {
                    player.Inventory.SetItem(index, null);
                    block_inv.TryAddItem(item);
                }
            }
            else
            {
                if (index < 9)
                {
                    if (player.Inventory.HasSpace(item, 9))
                    {
                        player.Inventory.SetItem(index, null);
                        player.Inventory.TryAddItem(item, 9);
                    }
                }
                else
                {
                    if (player.Inventory.HasSpace(item))
                    {
                        player.Inventory.SetItem(index, null);
                        player.Inventory.TryAddItem(item);
                    }
                }
            }
        }
    }
}