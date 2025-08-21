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
        static Dictionary<string, ComponentAndSystem> CMPSets = new();

        public static void ExecuteComponents(WorldEvent worldEvent, Blockdata blockdata)
        {
            int start_id = blockdata.ID;
            int start_state = blockdata.STATE;
            for (int i = 0; i < blockdata.components.Count; i++)
            {
                Component component = blockdata.components[i];
                if (component == null)
                {
                    //按理说不应该出现这个报错，如果出现了，得严查
                    GD.PrintErr("item is null");
                    blockdata.components.RemoveAt(i);
                    return;
                }

                if (CMPSets.ContainsKey(component.Name))
                    CMPSets[component.Name].system.Execute(worldEvent, component);
                //方块类型和状态已经被组件给修改了，防止其他组件异常执行，直接跳过之后的组件
                if (blockdata.ID != start_id || blockdata.STATE != start_state)
                    return;
            }
        }

        public static void Register(String key, Func<Component> func, IComponentSystem System)
        {
            CMPSets.Add(key, new ComponentAndSystem()
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
        }
    }
}