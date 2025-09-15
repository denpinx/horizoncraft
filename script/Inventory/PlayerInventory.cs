using Godot;
using MemoryPack;

namespace horizoncraft.script.Inventory;

[MemoryPackable]
public partial class PlayerInventory() : InventoryBase(4 * 9 + 4)
{
    /// <summary>
    /// 工具栏的手中物品
    /// </summary>
    public short ToolBarIndex = 0;

    /// <summary>
    /// 在打开背包时手持的物品
    /// </summary>
    public ItemStack HandItemStack;
    
    
    /// <summary>
    /// 获取物品栏中手中的物品
    /// </summary>
    /// <returns></returns>
    public ItemStack GetHandItemStack()
    {
        if (HandItemStack == null) return null;
        return HandItemStack.Amount <= 0 ? null : HandItemStack;
    }
    /// <summary>
    /// 获取工具栏中使用的物品
    /// </summary>
    /// <returns></returns>
    public ItemStack GetToolBarItem()
    {
        return GetItem(ToolBarIndex);
    }
}