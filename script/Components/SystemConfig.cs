using System;
using Horizoncraft.script.Components.Systems;

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
        public ComponentSystem System;
    }
}