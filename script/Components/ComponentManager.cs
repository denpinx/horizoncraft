using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components.Systems;
using horizoncraft.script.Events;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components
{
    using System;

    //Link & Execut,ECS
    public class ComponentManager
    {
        static Dictionary<string, ComponentAndSystem> ComponentSets = new();

        public static void ExecuteComponents(WorldEvent worldEvent, Blockdata blockdata)
        {
            int start_id = blockdata.ID;
            for (int i = 0; i < blockdata.components.Count; i++)
            {
                Component component = blockdata.components[i];
                if (component == null)
                {
                    GD.PrintErr("组件被异常删除!");
                    blockdata.components.RemoveAt(i);
                    return;
                }

                if (ComponentSets.ContainsKey(component.Name))
                    ComponentSets[component.Name].system.Execute(worldEvent, component);
                //方块类型和状态可能已经被组件给修改了，blockdata.componets的状态已经成为未知状态了,之后就不用继续运行
                if (blockdata.ID != start_id)
                    return;
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
            Register("BoxComponent", () => new InventoryComponent(), new TickSystem());
        }
    }
}