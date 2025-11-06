using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Events.SystemEvents;
using horizoncraft.script.Inventory;
using HorizonCraft.script.Services.world;

namespace horizoncraft.script.Components
{
    public class TickSystem : IComponentSystem
    {
        /// <summary>
        /// 处理放方块组件
        /// </summary>
        /// <param name="worldEvent"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public bool ExecuteBlockComponent(WorldEvent worldEvent, Component component)
        {
            if (worldEvent is BlockTickEvent)
            {
                if (component is InventoryComponent ic)
                    InventoryTick(worldEvent as BlockTickEvent, ic);
                else if (component is TickComponent tc)
                {
                    if (tc.Current == tc.Max)
                    {
                        BlockTick(worldEvent as BlockTickEvent, tc);
                        tc.Current = 0;
                    }
                    else if (tc.Current < tc.Max)
                    {
                        tc.Current++;
                    }
                }
                else if (component is ReactiveComponent rc)
                {
                    ReactiveTick(worldEvent as BlockTickEvent, rc);
                }
            }

            if (worldEvent is PlayerRightClickBlockEvent worldEvent2)
                return OnRightClick(worldEvent2, component);

            return true;
        }
        //接口原因，这里没用,
        public bool ExecuteItemComponent(PlayerEvent playerEvent, Component component)
        {
            return true;
        }

        public bool ExecuteEntityComponent(EntitySystemEvent ese)
        {
            foreach (var cmp in ese.EntityData.Components)
            {
                if (cmp is EntityComponent)
                {
                    var e = new EntityTickEvent()
                    {
                        UUID = ese.EntityData.Uuid,
                        WorldService = ese.Service,
                        EntityData = ese.EntityData,
                        EntityComponent = (EntityComponent)cmp,
                    };
                    EntityTick(e);
                }
            }

            return false;
        }

        /// <summary>
        /// 设置组件值事件
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="component">组件</param>
        /// <param name="value">值</param>
        public virtual void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
        {
        }

        /// <summary>
        /// 方块时刻更新
        /// </summary>
        /// <param name="blockTickEvent"></param>
        /// <param name="component"></param>
        public virtual void BlockTick(BlockTickEvent blockTickEvent, Component component)
        {
        }

        /// <summary>
        /// 容器的处理时刻
        /// </summary>
        /// <param name="blockTickEvent"></param>
        /// <param name="component"></param>
        public virtual void InventoryTick(BlockTickEvent blockTickEvent, InventoryComponent component)
        {
        }

        /// <summary>
        /// 实体时刻更新
        /// </summary>
        /// <param name="e"></param>
        public virtual void EntityTick(EntityTickEvent e)
        {
        }

        /// <summary>
        /// 被动更新
        /// </summary>
        /// <param name="blockTickEvent"></param>
        /// <param name="component"></param>
        public virtual void ReactiveTick(BlockTickEvent blockTickEvent, ReactiveComponent component)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerRightClickBlockEvent"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public virtual bool OnRightClick(PlayerRightClickBlockEvent playerRightClickBlockEvent, Component component)
        {
            return true;
        }
    }
}