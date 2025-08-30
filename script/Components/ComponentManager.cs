using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Events;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components;

public class ComponentManager
{
    static Dictionary<string, ComponentAndSystem> ComponentSets = new();

    public static bool ExecuteComponents(WorldEvent worldEvent, ItemStack itemStack)
    {
        int start_id = itemStack.Id;
        for (int i = 0; i < itemStack.Components.Count; i++)
        {
            Component component = itemStack.Components[i];
            if (component == null)
            {
                //特殊异常,必须手动解决，如果运行时出现问题，代表有严重bug要修复
                GD.PrintErr("组件被异常删除!");
                itemStack.Components.RemoveAt(i);
                return false;
            }
            if (ComponentSets.ContainsKey(component.Name))
            {
                //如果有任意一个组件取消了事件，之后的组件都不执行了
                var s = ComponentSets[component.Name].system.Execute(worldEvent, component);
                if (!s) return false;
            }
            if (itemStack.Id != start_id)
                return false;
        }

        return true;
    }

    public static bool ExecuteComponents(WorldEvent worldEvent, Blockdata blockdata)
    {
        int start_id = blockdata.Id;
        for (int i = 0; i < blockdata.components.Count; i++)
        {
            Component component = blockdata.components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                blockdata.components.RemoveAt(i);
                return false;
            }

            if (ComponentSets.ContainsKey(component.Name))
            {
                var s = ComponentSets[component.Name].system.Execute(worldEvent, component);
                //取消事件
                if (!s) return false;
            }

            //方块类型和状态可能已经被组件给修改了，blockdata.componets的状态已经成为未知状态了,之后就不用继续运行
            if (blockdata.Id != start_id)
                return false;
        }

        return true;
    }

    public static void SetBlockComponentData(PlayerData player, Blockdata blockdata,
        SetComponentData setComponentData)
    {
        for (int i = 0; i < blockdata.components.Count; i++)
        {
            Component component = blockdata.components[i];
            if (component == null)
            {
                GD.PrintErr("组件被异常删除!");
                blockdata.components.RemoveAt(i);
                return;
            }

            if (setComponentData.ComponentSets.ContainsKey(component.Name))
            {
                var d = setComponentData.ComponentSets[component.Name];
                ComponentSets[component.Name].system.SetComponentValue(player, component, d);
            }
            else
            {
                GD.Print("修改失败！不存在组件");
            }
        }
    }

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
    }
}