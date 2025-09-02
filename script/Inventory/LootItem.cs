using System.Collections.Generic;

namespace horizoncraft.script.Inventory;

/// <summary>
/// 由DropChance确定掉不掉落这个物品，再由AmountChances决定掉落的物品数量
/// </summary>
public class LootItem
{
    ///定义掉落的物品
    public ItemStack Item = new();

    ///定义掉落数量的概率
    public List<AmountChance> AmountChances = new();

    ///定义改物品掉落的概率 [0,1f]
    public float DropChance;
}

//等待所有配置文件加载完成后再转换为LootItem
public class LootItemSnapshot
{
    public string Name;
    public float DropChance;
    public List<AmountChance> AmountChances = new();
}

public class AmountChance
{
    ///[0,1f]
    public float Chance;

    ///any count
    public int Amount;
}