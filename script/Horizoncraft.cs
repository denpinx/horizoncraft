using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script
{
    public partial class Horizoncraft
    {
        static List<ManageAttribute> loadingManage = new();
        public static List<BaseManage> Manages = new();
        //观察者模式，确保在不侵入其他类型的情况下添加功能
        static Horizoncraft()
        {
            //ChunkManageSql是挂载在节点上的，依靠节点初始化完成后创建的，所以这里只是占位
            // loadingManage.Add(new ManageAttribute
            // {
            //     Name = "ChunkManageSql",
            //     Prerequisites = new(),
            //     GetManage = null,
            // });
            loadingManage.Add(new ManageAttribute
            {
                Name = "EntityManage",
                Prerequisites = new List<string> { "ChunkManageSql" },
                GetManage = () => new EntityManage(),
            });
        }
        public static void AddManage(BaseManage baseManage)
        {
            if (baseManage == null) return;
            if (Manages.Any(m => m.ManageName == baseManage.ManageName)) return;

            Manages.Add(baseManage);
            GD.Print($"{baseManage.ManageName} 已挂载");

            List<BaseManage> bases = new();
            for (int i = loadingManage.Count - 1; i >= 0; i--)
            {
                ManageAttribute manageAttribute = loadingManage[i];
                manageAttribute.Prerequisites.RemoveAll(p => p == baseManage.ManageName);
                if (manageAttribute.Prerequisites.Count == 0)
                {
                    bases.Add(manageAttribute.GetManage());
                    loadingManage.RemoveAt(i);
                }
            }
            foreach (BaseManage bm in bases) AddManage(bm);
        }
        public static T getManage<T>(string name) where T : BaseManage
        {
            for (int i = 0; i < Manages.Count; i++)
            {
                if (Manages[i].ManageName == name)
                    return (T)Manages[i];
            }
            return null;
        }
    }
}