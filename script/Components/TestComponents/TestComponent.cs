using System;
using System.Collections.Generic;

namespace Horizoncraft.script.Components.TestComponents;
[Obsolete]
public record TestComponent
{
    public string Name="";
    public int Age = 1;
    public Dictionary<string, string> Tags=new();
}