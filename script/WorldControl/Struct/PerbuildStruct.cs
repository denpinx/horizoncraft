using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace horizoncraft.script.WorldControl.Struct;

public class PreBuildStruct
{
    public string name;
    public System.Collections.Generic.Dictionary<Vector3I,PreBuildStructItem> blocks = new();

    public Dictionary ToDictionary()
    {
        Array array = new();
        foreach (var block in blocks.Values)
            array.Add(block.ToDictionary());
        return new Dictionary()
        {
            ["name"] = name,
            ["blocks"] = array
        };
    } 
}

public class PreBuildStructItem
{
    public int x;
    public int y;
    public int z;
    public string name;

    public Dictionary ToDictionary()
    {
        return new Dictionary()
        {
            ["x"] = x,
            ["y"] = y,
            ["z"] = z,
            ["name"] = name,
        };
    }
}