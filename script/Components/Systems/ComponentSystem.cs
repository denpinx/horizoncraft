using System.Collections.Generic;
using Horizoncraft.script.Events;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Events.SystemEvents;

namespace Horizoncraft.script.Components.Systems;

public abstract class ComponentSystem
{
    /// <summary>
    /// 处理方块组件
    /// </summary>
    /// <param name="worldEvent">方块事件</param>
    /// <param name="component">组件</param>
    /// <returns>结果</returns>
    public virtual bool ExecuteBlockComponent(WorldEvent worldEvent, Component component)
    {
        return true;
    }

    public virtual bool ExecuteRandBlockEvent(BlockTickEvent worldEvent, Component component)
    {
        return true;
    }

    /// <summary>
    /// 处理物品组件
    /// </summary>
    /// <param name="playerEvent">玩家事件</param>
    /// <param name="component">组件</param>
    /// <returns></returns>
    public virtual bool ExecuteItemComponent(PlayerEvent playerEvent, Component component)
    {
        return true;
    }

    /// <summary>
    /// 处理实体事件
    /// </summary>
    /// <param name="entitySystemEvent">事件名</param>
    /// <returns></returns>
    public virtual bool ExecuteEntityComponent(EntitySystemEvent entitySystemEvent)
    {
        return true;
    }

    /// <summary>
    /// 设置组件值
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="component">组件</param>
    /// <param name="value">字典</param>
    public virtual void SetComponentValue(PlayerData player, Component component, Dictionary<string, string> value)
    {
        return;
    }
}