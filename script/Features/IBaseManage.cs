using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HorizonCraft.script.WorldControl.Service;

namespace horizoncraft.script.Features
{
    public interface IBaseManage
    {
        public virtual WorldBase GetWorldBase()
        {
            return null;
        }
    }
}