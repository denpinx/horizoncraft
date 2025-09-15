using Godot.Collections;
using horizoncraft.script.Net;
using MemoryPack;

namespace horizoncraft.script.Config;

[MemoryPackable]
public partial class LocalProfile
{
    public string Name;

    public void ParseDictionary(Dictionary dict)
    {
        this.Name = (string)dict["Name"];
    }

    public Dictionary ToDictionary()
    {
        return new Dictionary()
        {
            ["Name"] = Name
        };
    }
}