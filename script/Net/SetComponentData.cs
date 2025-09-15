using System;
using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class SetComponentData
{
    public Dictionary<String, Dictionary<string, string>> ComponentSets = new();
    /// <summary>
    /// 添加组件设置
    /// </summary>
    /// <param name="componentName">组件名</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void AddComponentSet(string componentName, string key, string value)
    {
        if (!ComponentSets.ContainsKey(componentName))
            ComponentSets.Add(componentName, new Dictionary<string, string>());
        if (!ComponentSets[componentName].ContainsKey(key)) ComponentSets[componentName].Add(key, value);
        else ComponentSets[componentName][key] = value;
    }

    public bool HasComponentSet(string componentName)
    {
        return ComponentSets.ContainsKey(componentName);
    }
}