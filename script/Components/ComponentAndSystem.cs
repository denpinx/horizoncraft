using System;

namespace horizoncraft.script.Components
{
    public class ComponentAndSystem
    {
        /// <summary>
        /// 系统支持的组件类型
        /// </summary>
        public Type ComponentType;

        /// <summary>
        /// 组件对应的系统
        /// </summary>
        public IComponentSystem system;
    }
}