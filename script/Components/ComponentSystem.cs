using System.Collections.Generic;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Events.SystemEvents;

namespace horizoncraft.script.Components
{
    //组件事件处理
    public interface IComponentSystem
    {
        /// <summary>
        /// 处理方块组件
        /// </summary>
        /// <param name="worldEvent">方块事件</param>
        /// <param name="component">组件</param>
        /// <returns>结果</returns>
        bool ExecuteBlockComponent(WorldEvent worldEvent, Component component);

        /// <summary>
        /// 处理物品组件
        /// </summary>
        /// <param name="playerEvent">玩家事件</param>
        /// <param name="component">组件</param>
        /// <returns></returns>
        bool ExecuteItemComponent(PlayerEvent playerEvent, Component component);

        /// <summary>
        /// 处理实体事件
        /// </summary>
        /// <param name="entitySystemEvent">事件名</param>
        /// <returns></returns>
        bool ExecuteEntityComponent(EntitySystemEvent entitySystemEvent);

        /// <summary>
        /// 设置组件值
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="component">组件</param>
        /// <param name="value">字典</param>
        void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value);
    }
}