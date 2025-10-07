using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace horizoncraft.script.WorldControl.Struct;

public class PreBuildStruct
{
    public string name;
    public System.Collections.Generic.Dictionary<Vector3I, PreBuildStructItem> blocks = new();

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

    public Vector2I GetMaxPos()
    {
        if (blocks.Count == 0) return new Vector2I();
        var first = blocks.Keys.First();
        Vector2I result = new Vector2I(first.X, first.Y);
        foreach (var pos in blocks.Keys)
        {
            if (pos.X > result.X) result.X = pos.X;
            if (pos.Y > result.Y) result.Y = pos.Y;
        }

        return result + new Vector2I(1, 1);
    }

    public Vector2I GetMinPos()
    {
        var first = blocks.Keys.First();
        Vector2I result = new Vector2I(first.X, first.Y);
        foreach (var pos in blocks.Keys)
        {
            if (pos.X < result.X) result.X = pos.X;
            if (pos.Y < result.Y) result.Y = pos.Y;
        }

        return result;
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