using Godot;
using Godot.NativeInterop;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems.BlockSystems.Reactive;

public class TestReactiveSystem : TickSystem
{
    public override void ReactiveTick(BlockTickEvent blockTickEvent, ReactiveComponent component)
    {
        GD.Print("[被动更新]",blockTickEvent.BlockData.BlockMeta.Name);
    }
}