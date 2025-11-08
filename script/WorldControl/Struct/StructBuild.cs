using System;

namespace Horizoncraft.script.WorldControl.Struct;

public class StructBuild
{
    public string Name;

    public virtual BlockStruct DynamicBuild(int x, int y, int z, Random rand, params object[] args)
    {
        return null;
    }
}