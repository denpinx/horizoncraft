using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components.BlockComponents;
using horizoncraft.script.Components.EnergyBlocks;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Components.Systems.BlockSystems.EnergyBlocks;
using horizoncraft.script.Components.Systems.BlockSystems.Reactive;
using horizoncraft.script.Components.Systems.ItemSystems;
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
    /// <param name="entitySystemEvent">实体事件</param>
    /// <returns></returns>
    public static bool ExecuteEntityComponents(EntitySystemEvent entitySystemEvent)
    {
        foreach (var com in entitySystemEvent.EntityData.Components)
        {
            if (ComponentSets.TryGetValue(com.Name, out var value))
            {
                entitySystemEvent.EntityComponent = com as EntityComponent;
                var result = value.system.ExecuteEntityComponent(entitySystemEvent);
                if (!result) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 执行指定类型的物品组件。
    /// </summary>
    /// <param name="playerEvent">玩家事件</param>
    /// <param name="itemStack">物品</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns></returns>
    public static bool ExecuteItemComponents<T>(PlayerEvent playerEvent, ItemStack itemStack)
    {
        string start_id = itemStack.Name;
        for (int i = 0; i < itemStack.Components.Count; i++)
        {
            Component component = itemStack.Components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                itemStack.Components.RemoveAt(i);
                return false;
            }

            if (component is not T) continue;

            if (ComponentSets.TryGetValue(component.Name, out var value))
            {
                //如果有任意一个组件取消了事件，之后的组件都不执行了
                var s = value.system.ExecuteItemComponent(playerEvent, component);
                if (!s) return false;
            }

            if (itemStack.Name != start_id)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理物品组件事件
    /// </summary>
    /// <param name="playerEvent">玩家事件</param>
    /// <param name="itemStack">物品</param>
    /// <returns></returns>
    public static bool ExecuteItemComponents(PlayerEvent playerEvent, ItemStack itemStack)
    {
        string primName = itemStack.Name;
        for (int i = 0; i < itemStack.Components.Count; i++)
        {
            Component component = itemStack.Components[i];
            if (component == null)
            {
                itemStack.Components.RemoveAt(i);
                return false;
            }

            if (ComponentSets.TryGetValue(component.Name, out var value))
            {
                var s = value.system.ExecuteItemComponent(playerEvent, component);
                if (!s) return false;
            }

            if (itemStack.Name != primName)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理方块组件事件
    /// </summary>
    /// <param name="worldEvent">方块事件</param>
    /// <param name="blockData">方块</param>
    /// <returns></returns>
    public static bool ExecuteBlockComponents(WorldEvent worldEvent, BlockData blockData)
    {
        string start_id = blockData.Id;
        for (int i = 0; i < blockData.Components.Count; i++)
        {
            Component component = blockData.Components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                blockData.Components.RemoveAt(i);
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
    /// <param name="player">玩家</param>
    /// <param name="blockData">方块</param>
    /// <param name="setComponentData">组件配置</param>
    public static void SetBlockComponentData(PlayerData player, BlockData blockData,
        SetComponentData setComponentData)
    {
        for (int i = 0; i < blockData.Components.Count; i++)
        {
            Component component = blockData.Components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                blockData.Components.RemoveAt(i);
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
        //顶部方块覆盖组件
        Register("BlockCover",
            () => new ExpandComponent(),
            new BlockCoverSystem()
        );
        //方块扩散组件
        Register("BlockSpread",
            () => new ExpandComponent(),
            new BlockSpreadSystem()
        );
        //底部检查组件
        Register("BottomCheck",
            () => new TickComponent(),
            new BottomCheckSystem()
        );
        //流体组件，水流扩散
        Register("FluidComponent",
            () => new FluidComponent(),
            new FluidSystem()
        );
        //物理组件,模拟沙子掉落
        Register("PhysicsComponent",
            () => new PhysicsComponent(),
            new PhysicsSystem()
        );
        //箱子容器组件
        Register("BoxComponent",
            () => new InventoryComponent(),
            new InventorySystem()
        );
        //熔炉组件
        Register("FurnaceComponent",
            () => new FurnaceComponent(),
            new FurnaceSystem()
        );
        //物流输入组件
        Register("LogisticsInputComponent",
            () => new TickComponent(),
            new LogisticsInputSystem()
        );
        //工作台组件
        Register("WorkBenchComponent",
            () => new InventoryComponent(),
            new WorkBenchSystem()
        );
        //物品耐久&工具组件
        Register("ItemDurableComponent",
            () => new ItemDurableComponent(),
            new ItemDurableSystem()
        );
        //物品实体组件
        Register("ItemEntityComponent",
            () => new ItemEntityComponent(),
            new ItemEntitySystem()
        );
        //太阳能板组件
        Register("SolarGenerator",
            () => new EnergyUnitComponent(),
            new SolarGeneratorSystem()
        );
        //能量线缆组件
        Register("EnergyCable",
            () => new EnergyUnitComponent(),
            new EnergyCableSystem()
        );
        //植作物生长组件
        Register("CropGrowComponent",
            () => new CropGrowComponent(),
            new CropGrowthSystem()
        );
        //可食用物品组件
        Register(
            "ItemEatableComponent",
            () => new ItemEatableComponent(),
            new ItemEatableSystem()
        );

        //放置方块时的底部方块检查，确保方块被放置在了正确的方块之上。
        Register("BottomMatch",
            () => new BlockRelyOnComponent(),
            new PlaceBlockBottomMatchSystem()
        );


        //被动测试组件
        Register("TestReactiveSystem", () => new ReactiveComponent(), new TestReactiveSystem());
    }
}