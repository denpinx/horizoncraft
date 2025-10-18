using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace horizoncraft.script.Components.TestComponents;

public record TestComponent
{
    public string Name="";
    public int Age = 1;
    public Dictionary<string, string> Tags=new();
}