using System.Collections.Generic;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Utility;

namespace Horizoncraft.script;

public class NeoLootTable
{
    public Dictionary<string, LootTable> Loot = new Dictionary<string, LootTable>();

    public void LoadLootTables()
    {
        var loot_table_files = new List<string>();
        DirUtility.GetFiles("res://config/loot_table/",".json", loot_table_files);
    }
}