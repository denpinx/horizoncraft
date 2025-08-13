using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace horizoncraft.script.Features
{
    public class ManageAttribute
    {
        public List<String> Prerequisites;
        public String Name;
        public Func<BaseManage> GetManage;
    }
}