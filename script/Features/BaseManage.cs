using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace horizoncraft.script.Features
{
    public class BaseManage
    {
        public string ManageName;
        public BaseManage()
        {

        }
        public BaseManage(string name)
        {
            this.ManageName = name;
        }
        public virtual void OnLoad(List<BaseManage> baseManages)
        {

        }
    }
}