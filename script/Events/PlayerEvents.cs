using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.Recipes;
using HorizonCraft.script.Services.world;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Events;

public class PlayerEvents
{
    public virtual void SetOpenBlockComponent(PlayerSetBlockComponentEvent e)
    {
        var pos = new Vector3I((int)e.Player.OpenInventory.X, (int)e.Player.OpenInventory.Y,
            (int)e.Player.OpenInventory.Z);
        var block = e.world.Service.ChunkService.GetBlock(pos);
        if (block != null)
            ComponentManager.SetBlockComponentData(e.Player, block, e.ComponentData);
    }

    public virtual void CraftGridRecipeItem(PlayerCraftItemEvent e)
    {
        var gri = RecipeManage.GetRecipe(e.Player.Inventory, 2, 36);
        if (e.IsAllCraft)
        {
            while (gri != null)
            {
                var handitme = e.Player.Inventory.GetHandItemStack();
                if (handitme == null
                   )
                {
                    e.Player.Inventory.HandItemStack = gri.Result.Copy();
                }
                else if (
                    handitme.Id == gri.Result.Id &&
                    handitme.Amount + gri.Result.Amount <= gri.Result.GetItemMeta().MaxAmount
                )
                {
                    handitme.Amount += gri.Result.Amount;
                }
                else
                {
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    e.Player.Inventory.ReduceItemAmount(36 + i);
                }

                gri = RecipeManage.GetRecipe(e.Player.Inventory, 2, 36);
            }
        }
        else if (gri != null)
        {
            var handitme = e.Player.Inventory.GetHandItemStack();
            if (handitme == null
               )
            {
                e.Player.Inventory.HandItemStack = gri.Result.Copy();
            }
            else if (
                handitme.Id == gri.Result.Id &&
                handitme.Amount + gri.Result.Amount <= gri.Result.GetItemMeta().MaxAmount
            )
            {
                handitme.Amount += gri.Result.Amount;
            }
            else
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                e.Player.Inventory.ReduceItemAmount(36 + i);
            }
        }
    }

    public void OpenInventory(World world, string viewName)
    {
        if (world.PlayerNode.ShowView != null)
        {
            world.PlayerNode.RemoveChild(world.PlayerNode.ShowView);
        }
        
        world.PlayerNode.ShowView = InventoryManage.GetInventory<InventoryNode>(viewName);
        world.PlayerNode.ShowView.PlayerNode = world.PlayerNode;
        world.PlayerNode.AddChild(world.PlayerNode.ShowView);
    }

    public virtual bool ReciveLookingBlockData(PlayerOpenBlockViewEvent e)
    {
        var world = e.world;
        if (world.PlayerNode.ShowView != null)
        {
            world.PlayerNode.RemoveChild(world.PlayerNode.ShowView);
        }
        var block = world.Service.ChunkService.GetBlock(e.Position);
        if (block == null) return false;
        world.PlayerNode.ShowView = InventoryManage.GetInventory<InventoryNode>(e.ViewName);
        world.PlayerNode.ShowView.TargetBlock = block;
        world.PlayerNode.ShowView.TargetBlockGlobalPos = e.Position;
        world.PlayerNode.ShowView.PlayerNode = world.PlayerNode;
        world.PlayerNode.AddChild(world.PlayerNode.ShowView);
        return true;
    }

    public virtual bool OpenBlockView(PlayerOpenBlockViewEvent e)
    {
        var world = e.world;
        if (world.PlayerNode.ShowView != null)
        {
            world.PlayerNode.RemoveChild(world.PlayerNode.ShowView);
        }

        var block = world.Service.ChunkService.GetBlock(e.Position);
        if (block == null) return false;
        world.PlayerNode.ShowView = InventoryManage.GetInventory<InventoryNode>(e.ViewName);
        world.PlayerNode.ShowView.TargetBlock = block;
        world.PlayerNode.ShowView.TargetBlockGlobalPos = e.Position;
        world.PlayerNode.ShowView.PlayerNode = world.PlayerNode;
        world.PlayerNode.AddChild(world.PlayerNode.ShowView);
        return true;
    }

    public virtual bool PickItem(PlayerPickItemEvent e)
    {
        if (e.Inventory == null) return false;
        if (e.Player == null) return false;
        var handitem = e.Player.Inventory.GetHandItemStack();
        e.Inventory.update = true;
        var targetitem = e.GetIndexItem();
        if (targetitem != null && handitem != null && targetitem.Id == handitem.Id)
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

    public virtual bool PlaceBlock(PlayerPlaceBlockEvent e)
    {
        var pos0 = new Vector3I(e.Position.X, e.Position.Y, 0);
        var pos1 = new Vector3I(e.Position.X, e.Position.Y, 1);
        var block1 = e.ChunkService.GetBlock(pos0);
        var block2 = e.ChunkService.GetBlock(pos1);

        if (block1 == null || block2 == null) return false;

        Vector3I finalpos;
        BlockData finalblock;
        if (e.coercive)
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
                if (e.IsCollide) return false;
            }
        }

        if (!finalblock.IsMeta("air")) return false;

        var item = e.Player.Inventory.GetItem(e.Player.Inventory.HandSlot);
        if (item == null) return false;

        BlockMeta bm = item.GetBlockMeta();
        if (bm == null) return false;

        e.ChunkService.SetBlock(finalpos, bm);
        if (e.Player.Mode == 0) e.Player.Inventory.ReduceItemAmount(e.Player.Inventory.HandSlot);
        return true;
    }

    public virtual bool BreakBlock(PlayerBreakblockEvent e)
    {
        var targetblock = e.ChunkService.GetBlock(e.Position);


        if (e.Player.Mode == 0)
        {
            if (e.ChunkService.CheckIsCloseBlock(e.Position) || targetblock == null || targetblock.IsMeta("air"))
                return false;
            var item = targetblock.BlockMeta?.ItemMeta?.GetItemStack();
            if (item != null)
            {
                var handItem = e.Player.Inventory.GetItem(e.Player.Inventory.HandSlot);
                if (handItem != null && handItem.Components.Count > 0)
                {
                    ComponentManager.ExecuteComponents(e, handItem);
                }

                e.Player.Inventory.TryAddItem(item);
            }
            else
            {
                GD.PrintErr($"物品不存在！ :{targetblock.BlockMeta.Name}");
            }
        }

        e.ChunkService.SetBlock(e.Position, Materials.Valueof("air"));
        return true;
    }

    public virtual bool InterfaceBlock(InterfaceBlockEvent e)
    {
        var pos0 = new Vector3I(e.Position.X, e.Position.Y, 0);
        var pos1 = new Vector3I(e.Position.X, e.Position.Y, 1);
        var block1 = e.ChunkService.GetBlock(pos0);
        var block2 = e.ChunkService.GetBlock(pos1);

        if (block1 == null || block2 == null) return false;

        Vector3I finalpos;
        BlockData InterfaceBlock;
        if (!block2.IsMeta("air"))
        {
            finalpos = pos1;
            InterfaceBlock = block2;
        }
        else
        {
            finalpos = pos0;
            InterfaceBlock = block1;
        }

        if (InterfaceBlock.IsMeta("air")) return false;

        e.Player.OpeningBlockInventory = true;
        e.Player.OpenInventory = new System.Numerics.Vector3(finalpos.X, finalpos.Y, finalpos.Z);
        var blockinv = InterfaceBlock.GetComponent<InventoryComponent>();
        if (blockinv == null) return false;
        var pobve = new PlayerOpenBlockViewEvent()
        {
            Player = e.Player,
            world = e.world,
            Position = e.Position,
            ViewName = blockinv.InventoryName
        };

        e.PlayerService.Events.OpenBlockView(pobve);
        return true;
    }

    public virtual void CloseInventory(WorldServiceBase service, string name)
    {
        if (service.PlayerService.Players.TryGetValue(name, out var player))
        {
            player.OpeningBlockInventory = false;
        }
    }
}