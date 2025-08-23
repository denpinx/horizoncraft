using System;
using System.Collections.Generic;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class SetComponentData
{
    public Dictionary<String, Dictionary<string, string>> ComponentSets = new();

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