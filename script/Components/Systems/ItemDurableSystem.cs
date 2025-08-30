using Godot;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Events;

namespace horizoncraft.script.Components.Systems;

public class ItemDurableSystem : ItemComponentSystem
{
    public override bool OnBreakBlock(BreakBlockEvent bbe, ItemComponent itemComponent)
    {
        var durable = itemComponent as ItemDurableComponent;
        GD.Print($"[方块挖掘] 当前耐久{durable.Value}/{durable.Max}");
        if (bbe.Blockdata.BlockMeta.BreakLevel <= durable.ToolLevel)
        {
            //正常掉落
            bbe.DropItemStack = bbe.Blockdata.BlockMeta.ItemMeta.GetItemStack();
        }
        else
        {
            //无掉落物
            bbe.DropItemStack = null;
        }

        durable.Value -= 1;
        //这里Itemstack.Amount<=0时之后被获取时会被直接替换成null的
        //当前的组件是保存在这个ItemStack里面的,所以这样设置最安全。
        //支持耐久物品叠加
        if (durable.Value <= 0)
        {
            durable.Value = durable.Max;
            bbe.ItemStack.Amount -= 1;
        }

        return true;
    }
}