using Godot;
using Godot.NativeInterop;

namespace horizoncraft.script.Services;

public class ServiceBase(World world)
{
    protected World World = world;
}