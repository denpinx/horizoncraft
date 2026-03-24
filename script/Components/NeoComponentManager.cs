using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Components.EnergyBlocks;
using Horizoncraft.script.Components.EntityComponents;
using Horizoncraft.script.Components.Item;
using Horizoncraft.script.Components.Systems;
using Horizoncraft.script.Components.Systems.BlockSystems;
using Horizoncraft.script.Components.Systems.BlockSystems.EnergyBlocks;
using Horizoncraft.script.Components.Systems.BlockSystems.Reactive;
using Horizoncraft.script.Components.Systems.ItemSystems;
using Horizoncraft.script.Events;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Events.SystemEvents;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Net;
using Horizoncraft.script.Services.world;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Components;

public class NeoComponentManager
{
    private readonly Dictionary<SystemEnum, SystemConfig> ComponentSets = new();

    /// <summary>
    /// 处理实体组件事件
    /// </summary>
    /// <param name="entitySystemEvent">实体事件</param>
    /// <returns>是否有组件的系统取消事件</returns>
    public bool ExecuteEntityComponents(EntitySystemEvent entitySystemEvent)
    {
        foreach (var com in entitySystemEvent.EntityData.Components)
        {
            if (ComponentSets.TryGetValue(com.EnumId, out var value))
            {
                entitySystemEvent.EntityComponent = com as EntityComponent;
                var result = value.System.ExecuteEntityComponent(entitySystemEvent);
                if (!result) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 执行指定类型的物品组件。
    /// </summary>
    /// <param name="playerEvent">玩家事件</param>
    /// <param name="itemStack">物品</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>是否有组件的系统取消事件</returns>
    public bool ExecuteItemComponents<T>(PlayerEvent playerEvent, ItemStack itemStack)
    {
        string startId = itemStack.Name;
        foreach (var component in itemStack.Components)
        {
            if (component == null)
            {
                GD.PrintErr($"[{nameof(NeoComponentManager)}] {nameof(ExecuteItemComponents)} 物品组件被意外删除。");
                GD.PrintErr($"\t item-name:\t{itemStack.Name}");
                GD.PrintErr($"\t item-state:\t{itemStack.State}");
                GD.PrintErr($"\t item-components:\t{string.Join(",", itemStack.Components.Select(c => c.Drive))}");
                continue;
            }

            if (component is not T) continue;

            if (ComponentSets.TryGetValue(component.EnumId, out var value))
            {
                //如果有任意一个组件取消了事件，之后的组件都不执行了
                var s = value.System.ExecuteItemComponent(playerEvent, component);
                if (!s) return false;
            }

            if (itemStack.Name != startId)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理物品组件事件
    /// </summary>
    /// <param name="playerEvent">玩家事件</param>
    /// <param name="itemStack">物品</param>
    /// <returns>是否有组件的系统取消事件</returns>
    public bool ExecuteItemComponents(PlayerEvent playerEvent, ItemStack itemStack)
    {
        string primName = itemStack.Name;
        foreach (var component in itemStack.Components)
        {
            if (component == null)
            {
                GD.PrintErr($"[{nameof(NeoComponentManager)}] {nameof(ExecuteItemComponents)} 物品组件被意外删除。");
                GD.PrintErr($"\t item-name:\t{itemStack.Name}");
                GD.PrintErr($"\t item-state:\t{itemStack.State}");
                GD.PrintErr($"\t item-components:\t{string.Join(",", itemStack.Components.Select(c => c.Drive))}");
                continue;
            }

            if (ComponentSets.TryGetValue(component.EnumId, out var value))
            {
                var s = value.System.ExecuteItemComponent(playerEvent, component);
                if (!s) return false;
            }

            if (itemStack.Name != primName)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理随机方块组件事件
    /// </summary>
    /// <param name="tickEvent">方块事件</param>
    /// <param name="blockData">方块</param>
    /// <returns>是否有组件的系统取消事件或执行失败</returns>
    public bool ExecuteRandBlockComponents(BlockTickEvent tickEvent, BlockData blockData)
    {
        string startId = blockData.Id;
        foreach (var component in blockData.Components)
        {
            if (component == null)
            {
                GD.PrintErr($"[{nameof(NeoComponentManager)}] {nameof(ExecuteBlockComponents)} 方块组件被意外删除。");
                GD.PrintErr($"\t block-name:\t{blockData.BlockMeta.Name}");
                GD.PrintErr($"\t block-state:\t{blockData.State}");
                GD.PrintErr(
                    $"\t block-runtime-components:\t{string.Join(",", blockData.Components.Select(c => c.Drive))}");
                GD.PrintErr(
                    $"\t block-original-components:\t{string.Join(",", blockData.BlockMeta.Examples.Select(c => c.Drive))}");
                continue;
            }

            if (ComponentSets.TryGetValue(component.EnumId, out var set))
            {
                var s = set.System.ExecuteRandBlockEvent(tickEvent, component);
                if (!s) return false;
            }

            if (blockData.Id != startId)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理方块组件事件
    /// </summary>
    /// <param name="worldEvent">方块事件</param>
    /// <param name="blockData">方块</param>
    /// <returns>是否有组件的系统取消事件或执行失败</returns>
    public bool ExecuteBlockComponents(WorldEvent worldEvent, BlockData blockData)
    {
        string startId = blockData.Id;
        foreach (var component in blockData.Components)
        {
            worldEvent.Reset();

            if (component == null)
            {
                GD.PrintErr($"[{nameof(NeoComponentManager)}] {nameof(ExecuteBlockComponents)} 方块组件被意外删除。");
                GD.PrintErr($"\t block-name:\t{blockData.BlockMeta.Name}");
                GD.PrintErr($"\t block-state:\t{blockData.State}");
                GD.PrintErr(
                    $"\t block-runtime-components:\t{string.Join(",", blockData.Components.Select(c => c.Drive))}");
                GD.PrintErr(
                    $"\t block-original-components:\t{string.Join(",", blockData.BlockMeta.Examples.Select(c => c.Drive))}");

                continue;
            }

            if (ComponentSets.TryGetValue(component.EnumId, out var set))
            {
                var s = set.System.ExecuteBlockComponent(worldEvent, component);
                //取消事件
                if (!s) return false;
            }

            //方块类型和状态可能已经被组件给修改了,blockdata.componets的状态已经成为未知状态了,之后就不用继续运行
            if (blockData.Id != startId)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 设置方块组件数据,通常用来给客户端玩家用菜单远程配置服务端的某个组件值用的。
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="blockData">方块</param>
    /// <param name="setComponentData">组件配置</param>
    public void SetBlockComponentData(PlayerData player, BlockData blockData,
        SetComponentData setComponentData)
    {
        foreach (var component in blockData.Components)
        {
            if (component == null)
            {
                GD.PrintErr($"[{nameof(NeoComponentManager)}] {nameof(SetBlockComponentData)} 方块组件被意外删除。");
                GD.PrintErr($"\t block-name:\t{blockData.BlockMeta.Name}");
                GD.PrintErr($"\t block-state:\t{blockData.State}");
                GD.PrintErr(
                    $"\t block-runtime-components:\t{string.Join(",", blockData.Components.Select(c => c.Drive))}");
                GD.PrintErr(
                    $"\t block-original-components:\t{string.Join(",", blockData.BlockMeta.Examples.Select(c => c.Drive))}");
                continue;
            }

            if (setComponentData.ComponentSets.TryGetValue(component.Drive, out var dict))
            {
                if (ComponentSets.TryGetValue(component.EnumId, out var cmp))
                    cmp.System.SetComponentValue(player, component, dict);
                else
                {
                    GD.PrintErr($"[{nameof(NeoComponentManager)}] {nameof(SetBlockComponentData)} 没有对应的组件处理该方法。");
                    GD.PrintErr($"\t component-name:\t{component.Drive}");
                }
            }
        }
    }

    /// <summary>
    /// 注册组件功能
    /// </summary>
    /// <param name="enumId">枚举Id</param>
    /// <param name="componentType">功能服务的组件类型</param>
    /// <param name="system">系统</param>
    private void Register(SystemEnum enumId, Type componentType, ComponentSystem system)
    {
        ComponentSets.Add(enumId, new SystemConfig()
        {
            MatchType = componentType,
            System = system,
        });
    }

    //绑定组件功能和组件类型，
    //注意：有些组件会在事件触发时修改物品或方块状态，不是所有组件都能够相互兼容。
    public NeoComponentManager(WorldServiceBase worldServiceBase)
    {
        //顶部方块覆盖组件
        Register(SystemEnum.BlockCover,
            typeof(ExpandComponent),
            new BlockCoverSystem()
        );
        //方块扩散组件
        Register(SystemEnum.BlockSpread,
            typeof(ExpandComponent),
            new BlockSpreadSystem()
        );
        //底部检查组件
        Register(SystemEnum.BottomCheck,
            typeof(BottomCheckComponent),
            new BottomCheckSystem()
        );
        //流体组件，水流扩散
        Register(SystemEnum.FluidComponent,
            typeof(FluidComponent),
            new FluidSystem()
        );
        //物理组件,模拟沙子掉落
        Register(SystemEnum.PhysicsComponent,
            typeof(PhysicsComponent),
            new PhysicsSystem()
        );
        //箱子容器组件
        Register(SystemEnum.BoxComponent,
            typeof(InventoryComponent),
            new InventorySystem()
        );
        //熔炉组件
        Register(SystemEnum.FurnaceComponent,
            typeof(FurnaceComponent),
            new FurnaceSystem()
        );
        //物流输入组件
        Register(SystemEnum.LogisticsInputComponent,
            typeof(TickComponent),
            new LogisticsInputSystem()
        );
        //工作台组件
        Register(SystemEnum.WorkBenchComponent,
            typeof(InventoryComponent),
            new WorkBenchSystem()
        );
        //物品耐久&工具组件
        Register(SystemEnum.ToolSystem,
            typeof(ToolComponent),
            new ItemDurableSystem()
        );
        //物品实体组件
        Register(SystemEnum.ItemEntityComponent,
            typeof(ItemEntityComponent),
            new ItemEntitySystem()
        );
        //太阳能板组件
        Register(SystemEnum.SolarGenerator,
            typeof(EnergyUnitComponent),
            new SolarGeneratorSystem()
        );
        //能量线缆组件
        Register(SystemEnum.EnergyCable,
            typeof(EnergyUnitComponent),
            new EnergyCableSystem()
        );
        //植作物生长组件
        Register(SystemEnum.CropGrowComponent,
            typeof(CropGrowComponent),
            new CropGrowthSystem()
        );
        //可食用物品组件
        Register(
            SystemEnum.ItemEatableComponent,
            typeof(ItemEatableComponent),
            new ItemEatableSystem()
        );
        //放置方块时的底部方块检查，确保方块被放置在了正确的方块之上。
        Register(SystemEnum.BottomMatch,
            typeof(BlockRelyOnComponent),
            new PlaceBlockBottomMatchSystem()
        );
        //门-联动更新系统，门的状态被更新时，自动一起关系，门的结构被破坏时自动消失另一部分。
        Register(SystemEnum.DoorLinkPlace,
            typeof(ItemComponent),
            new DoorLinkPlaceSystem()
        );
        //门-联动放置系统，放置门时自动防止上半部分。
        Register(SystemEnum.DoorLinkUpdate,
            typeof(ReactiveComponent),
            new DoorLinkUpdateSystem()
        );
        //让物品能够消耗自己放置流体.
        Register(SystemEnum.PlaceFluidSystem,
            typeof(ItemFluidComponent),
            new PlaceFluidSystem()
        );
        //竹子生长系统
        Register(SystemEnum.BambooSystem,
            typeof(ReactiveComponent),
            new BambooSystem()
        );
        //竹笋生长系统
        Register(SystemEnum.BambooShootSystem,
            typeof(ReactiveComponent),
            new BambooShootSystem()
        );
        //芦苇生长系统
        Register(SystemEnum.ReedSystem,
            typeof(ReactiveComponent),
            new ReedSystem()
        );
        //水缸系统
        Register(SystemEnum.TankSystem,
            typeof(TankComponent),
            new IronTankSystem()
        );
        //流体管道
        Register(SystemEnum.FluidPipeSystem,
            typeof(ConnectComponent),
            new FluidPipeSystem()
        );
        //仙人掌生长组件
        Register(SystemEnum.CactusSystem,
            typeof(ReactiveComponent),
            new CactusSystem()
        );
        
        ComponentSystemInitialize csi = new ComponentSystemInitialize()
        {
            WorldService = worldServiceBase,
        };
        foreach (var componentSet in  ComponentSets)
            componentSet.Value.System.Initialize(csi);
        
    }
}