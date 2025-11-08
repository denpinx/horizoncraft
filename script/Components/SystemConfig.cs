using System;

namespace Horizoncraft.script.Components
{
    public class SystemConfig
    {
        /// <summary>
        /// 系统支持的组件类型
        /// </summary>
        public Type MatchType;

        /// <summary>
        /// 组件对应的系统
        /// </summary>
        public IComponentSystem System;
    }
}