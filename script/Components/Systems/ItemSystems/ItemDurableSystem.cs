using Godot;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;

namespace horizoncraft.script.Components.Systems;

public class ItemDurableSystem : ItemComponentSystem
{
    public override bool OnBreakBlock(PlayerBreakblockEvent bbe, ItemComponent itemComponent)
    {
        var durable = itemComponent as ItemDurableComponent;
        GD.Print($"[方块挖掘] 当前耐久{durable.Value}/{durable.Max}");
        if (bbe.GetBlockData().BlockMeta.BreakLevel <= durable.ToolLevel)
            //正常掉落
            bbe.DropLoots = bbe.GetBlockData().BlockMeta.LootTable.TryTakeItem(bbe.GetBlockData().State);


        durable.Value -= 1;
        //这里Itemstack.Amount<=0时之后被获取时会被直接替换成null的
        //当前的组件是保存在这个ItemStack里面的,所以这样设置最安全。
        //支持耐久物品叠加
        if (durable.Value <= 0)
        {
            durable.Value = durable.Max;
            bbe.GetItemStack().Amount -= 1;
        }

        return true;
    }
}