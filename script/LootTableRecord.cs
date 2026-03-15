using System.Collections.Generic;
using System.Text.Json.Serialization;
using Horizoncraft.script.Inventory;

namespace Horizoncraft.script;

public record LootTableRecord
{
    [JsonPropertyName("table-name")] public string TableName { set; get; } = "";
    [JsonPropertyName("loot")] public List<LootItemRecord> Loot { set; get; } = new();
}

public record LootItemRecord
{
    [JsonPropertyName("name")] public string Name { set; get; } = "";
    [JsonPropertyName("drop-chance")] public float DropChance { set; get; } = 1;
    [JsonPropertyName("drop-state")] public int DropState { set; get; } = -1;
    [JsonPropertyName("amount-chance")] public List<AmountChanceRecord> AmountChances { set; get; } = new();
}

public record AmountChanceRecord
{
    [JsonPropertyName("chance")] public float Chance { set; get; }
    [JsonPropertyName("amount")] public int Amount { set; get; }
}