using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components.EnergyBlocks;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Components.Systems.BlockSystems.EnergyBlocks;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Events.SystemEvents;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components;

public static class ComponentManager
{
    private static readonly Dictionary<string, ComponentAndSystem> ComponentSets = new();

    /// <summary>
    /// 处理实体组件事件
    /// </summary>
    /// <param name="ese"></param>
    /// <returns></returns>
    public static bool ExecuteEntityComponents(EntitySystemEvent ese)
    {
        foreach (var com in ese.EntityData.Components)
        {
            if (ComponentSets.TryGetValue(com.Name, out var value))
            {
                ese.EntityComponent = com as EntityComponent;
                var result = value.system.ExecuteEntityComponent(ese);
                if (!result) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 处理物品组件事件
    /// </summary>
    /// <param name="playerEvent"></param>
    /// <param name="itemStack"></param>
    /// <returns></returns>
    public static bool ExecuteItemComponents(PlayerEvent playerEvent, ItemStack itemStack)
    {
        int start_id = itemStack.Id;
        for (int i = 0; i < itemStack.Components.Count; i++)
        {
            Component component = itemStack.Components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                itemStack.Components.RemoveAt(i);
                return false;
            }

            if (ComponentSets.TryGetValue(component.Name, out var value))
            {
                //如果有任意一个组件取消了事件，之后的组件都不执行了
                var s = value.system.ExecuteItemComponent(playerEvent, component);
                if (!s) return false;
            }

            if (itemStack.Id != start_id)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理方块组件事件
    /// </summary>
    /// <param name="worldEvent"></param>
    /// <param name="blockData"></param>
    /// <returns></returns>
    public static bool ExecuteBlockComponents(WorldEvent worldEvent, BlockData blockData)
    {
        int start_id = blockData.Id;
        for (int i = 0; i < blockData.components.Count; i++)
        {
            Component component = blockData.components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                blockData.components.RemoveAt(i);
                return false;
            }

            if (ComponentSets.ContainsKey(component.Name))
            {
                var s = ComponentSets[component.Name].system.ExecuteBlockComponent(worldEvent, component);
                //取消事件
                if (!s) return false;
            }

            //方块类型和状态可能已经被组件给修改了，blockdata.componets的状态已经成为未知状态了,之后就不用继续运行
            if (blockData.Id != start_id)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 设置方块组件数据
    /// </summary>
    /// <param name="player"></param>
    /// <param name="blockData"></param>
    /// <param name="setComponentData"></param>
    public static void SetBlockComponentData(PlayerData player, BlockData blockData,
        SetComponentData setComponentData)
    {
        for (int i = 0; i < blockData.components.Count; i++)
        {
            Component component = blockData.components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                blockData.components.RemoveAt(i);
                return;
            }

            if (setComponentData.ComponentSets.TryGetValue(component.Name, out var dict))
            {
                ComponentSets[component.Name].system.SetComponentValue(player, component, dict);
            }
            else
            {
                GD.Print("修改失败！不存在组件");
            }
        }
    }

    /// <summary>
    /// 注册组件功能
    /// </summary>
    /// <param name="key">功能名</param>
    /// <param name="func">组件</param>
    /// <param name="System">处理方法</param>
    public static void Register(String key, Func<Component> func, IComponentSystem System)
    {
        ComponentSets.Add(key, new ComponentAndSystem()
        {
            GetComponect = func,
            system = System,
        });
    }

    //绑定组件功能和组件类型
    static ComponentManager()
    {
        Register("BlockCover", () => new ExpandComponent(), new BlockCoverSystem());
        Register("BlockSpread", () => new ExpandComponent(), new BlockSpreadSystem());
        Register("BottomCheck", () => new TickComponent(), new BottomCheckSystem());
        Register("FluidComponent", () => new FluidComponent(), new FluidSystem());
        Register("PhysicsComponent", () => new PhysicsComponent(), new PhysicsSystem());
        Register("BoxComponent", () => new InventoryComponent(), new InventorySystem());
        Register("FurnaceComponent", () => new FurnaceComponent(), new FurnaceSystem());
        Register("LogisticsInputComponent", () => new TickComponent(), new LogisticsInputSystem());
        Register("WorkBenchComponent", () => new InventoryComponent(), new WorkBenchSystem());
        Register("ItemDurableComponent", () => new ItemDurableComponent(), new ItemDurableSystem());
        Register("ItemEntityComponent", () => new ItemEntityComponent(), new ItemEntitySystem());
        Register("SolarGenerator", () => new EnergyUnitComponent(), new SolarGeneratorSystem());
        Register("EnergyCable", () => new EnergyUnitComponent(), new EnergyCableSystem());
    }
}