using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Utility;

namespace Horizoncraft.script;

public class NeoLootTable()
{
    public Dictionary<string, LootTable> LootTables = new();

    /// <summary>
    /// 加载位于 "res://config/loot_table" 中的战利品表配置
    /// </summary>
    public void LoadLootTables()
    {
        var loot_table_files = new List<string>();
        DirUtility.GetFiles("res://config/loot_table", ".json", loot_table_files);
        foreach (var loot_table_file_path in loot_table_files)
        {
            FileAccess file_access = FileAccess.Open(loot_table_file_path, FileAccess.ModeFlags.Read);
            string json = file_access.GetAsText();
            var tableRecord = System.Text.Json.JsonSerializer.Deserialize<LootTableRecord>(json);
            LootTable loot_table = new LootTable();
            foreach (var table_item in tableRecord.Loot)
            {
                var item = Materials.ItemMetas[table_item.Name];
                if (item == null)
                {
                    GD.PrintErr($"{nameof(NeoLootTable)} 没有物品 {table_item.Name}");
                    continue;
                }

                LootItem loot_item = new LootItem();
                loot_item.Item = item.GetItemStack();
                loot_item.DropChance = table_item.DropChance;
                loot_item.DropState = table_item.DropState;
                foreach (var chances in table_item.AmountChances)
                {
                    loot_item.AmountChances.Add(new AmountChance()
                    {
                        Chance = chances.Chance,
                        Amount = chances.Amount,
                    });
                }

                loot_table.LootItems.Add(loot_item);
            }

            LootTables.Add(tableRecord.TableName, loot_table);
            GD.Print($"加载战利品表：{tableRecord.TableName} {loot_table_file_path}");
        }
    }

    /// <summary>
    /// 为全部BlockMeta生成对应的战利品表
    /// </summary>
    public void GenerateBlockMetaLootTables()
    {
        foreach (var meta in Materials.BlockMetas.Values)
        {
            if (meta.LootTableName == null)
            {
                var Table = new LootTable();
                LootItem lootItem = new LootItem();
                lootItem.Item = meta.ItemMeta.GetItemStack();
                lootItem.DropChance = 1;
                lootItem.AmountChances.Add(new AmountChance()
                {
                    Amount = 1,
                    Chance = 1,
                });
                Table.LootItems.Add(lootItem);
                var name = $"AutoGenerate::{meta.Name}";
                LootTables.Add(name, Table);
                GD.Print("NeoLootTable 自动生成配方:" + name);
                meta.LootTableName = name;
            }
        }
    }

    /// <summary>
    /// 获取战利品表
    /// </summary>
    /// <param name="loot_table_name">表名</param>
    /// <returns></returns>
    public LootTable GetLootTable(string loot_table_name)
    {
        if (LootTables.TryGetValue(loot_table_name, out var loot_table))
        {
            return loot_table;
        }

        GD.PrintErr($"{nameof(NeoLootTable)} 没有战利品表 {loot_table_name}");
        return null;
    }
}