using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace horizoncraft.script.WorldControl
{
    /**
     * <summary>在区块未加载之前存储的对该区块执行的操作，区块加载后执行</summary>
     */
    public class WorkBase
    {
        public string Type;

        public WorkBase()
        {
            Type = "NONE";
        }

        public virtual void Execute(Chunk chunk)
        {
        }
    }
}