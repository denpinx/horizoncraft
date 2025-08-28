using System;
using Godot;

namespace horizoncraft.script.WorldControl.Struct;

public class StructBuild
{
    public string Name;

    public virtual BlockStruct DynamicBuild(int x, int y, int z, Random rand)
    {
        return null;
    }
}