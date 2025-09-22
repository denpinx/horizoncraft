using Godot;
using Godot.Collections;

namespace horizoncraft.script.resource;
[GlobalClass]
public partial class ComponentsResource:Resource
{
    [Export]public string ComponentName;
    [Export]public Dictionary<string,Variant> ComponentData;
}