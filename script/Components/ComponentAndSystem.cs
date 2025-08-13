using System;
namespace horizoncraft.script.Components
{
    public class ComponentAndSystem
    {
        //用于给配置文件创建组件实例
        public Func<Component> GetComponect;
        //组件绑定的调用方法
        public IComponentSystem system;
    }
}