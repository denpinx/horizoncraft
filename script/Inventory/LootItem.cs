using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Horizoncraft.script.Inventory;

/// <summary>
/// 由DropChance确定掉不掉落这个物品，再由AmountChances决定掉落的物品数量
/// </summary>
public class LootItem
{
    ///定义掉落的物品
    public ItemStack Item = new();

    ///定义掉落数量的概率
    public List<AmountChance> AmountChances = new();
    public int DropState = -1;
    ///定义改物品掉落的概率 [0,1f]
    public float DropChance;
}

//等待所有配置文件加载完成后再转换为LootItem
public class LootItemSnapshot
{
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("drop-chance")]
    public float DropChance{ get; set; } = 1;
    [JsonPropertyName("drop-state")]
    public int DropState { get; set; }= -1;
    [JsonPropertyName("amount-chance")]
    public List<AmountChance> AmountChances{ get; set; } = new();
}

public class AmountChance
{
    [JsonPropertyName("chance")]
    public float Chance{ get; set; }

    [JsonPropertyName("amount")]
    public int Amount{ get; set; }
}