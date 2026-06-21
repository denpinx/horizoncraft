using MemoryPack;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Components.EnergyBlocks;
using Horizoncraft.script.Components.EntityComponents;
using Horizoncraft.script.Components.Item;

namespace Horizoncraft.script.Components;

/// <summary>
/// 所有凡是继承了 Component 的组件都要在这里配置Union，否则无法序列化
/// </summary>
[MemoryPackable]
[MemoryPackUnion(0, typeof(TickComponent))]
[MemoryPackUnion(1, typeof(ExpandComponent))]
[MemoryPackUnion(2, typeof(FluidComponent))]
[MemoryPackUnion(3, typeof(PhysicsComponent))]
[MemoryPackUnion(4, typeof(ItemComponent))]
[MemoryPackUnion(5, typeof(InventoryComponent))]
[MemoryPackUnion(6, typeof(FurnaceComponent))]
[MemoryPackUnion(7, typeof(ToolComponent))]
[MemoryPackUnion(8, typeof(EntityComponent))]
[MemoryPackUnion(9, typeof(ItemEntityComponent))]
[MemoryPackUnion(10, typeof(EnergyUnitComponent))]
[MemoryPackUnion(11, typeof(ReactiveComponent))]
[MemoryPackUnion(12, typeof(CropGrowComponent))]
[MemoryPackUnion(13, typeof(ItemUsefulComponent))]
[MemoryPackUnion(14, typeof(ItemEatableComponent))]
[MemoryPackUnion(15, typeof(BlockRelyOnComponent))]
[MemoryPackUnion(16, typeof(ItemFluidComponent))]
[MemoryPackUnion(17, typeof(BottomCheckComponent))]
[MemoryPackUnion(18, typeof(ExpandReactiveComponent))]
[MemoryPackUnion(19, typeof(ConnectComponent))]
[MemoryPackUnion(20, typeof(TankComponent))]
[MemoryPackUnion(21, typeof(ExplosiveComponent))]
public abstract partial class Component
{
    [MemoryPackAllowSerialize] private string _drive;

    [MemoryPackIgnore]
    public string Drive
    {
        get => _drive;
        init
        {
            _drive = value;
            if (SystemEnum.TryParse<SystemEnum>(value, out var id))
            {
                EnumId = id;
            }
        }
    }

    public SystemEnum EnumId;
}