using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Inventory;

namespace Horizoncraft.script;

public class ItemMaterialsTemplate
{
    [JsonPropertyName("max")] public int Max { set; get; } = 1;
    [JsonPropertyName("description")] public string Description { set; get; }

    [JsonPropertyName("tags")] public Dictionary<string, string> Tags { get; set; } = new();
    [JsonPropertyName("state")] public Dictionary<string, string> State { get; set; } = new();

    [JsonPropertyName("components")]
    public Dictionary<string, System.Text.Json.JsonElement> Components { set; get; } = new();

    [JsonPropertyName("textures")] public List<string> Textures { set; get; } = new();

    public ItemMeta BuildItemMeta(string itemName)
    {
        ItemMeta itemMeta = new ItemMeta();
        itemMeta.Name = itemName;
        itemMeta.MaxAmount = Max;
        itemMeta.Description = Description;
        foreach (string cmp_name in Components.Keys)
        {
            GD.Print("[ItemMaterialsTemplate] 创建组件构造Lambda:" + cmp_name);
            itemMeta.Components.Add(
                LambdaCreater.CreateLambda<Component>(cmp_name,
                    (Dictionary<string, object>)JsonCleaner.ConvertRoot(Components[cmp_name]))
            );
        }
        itemMeta.Itemset = new ItemStateSet();
        if (Textures.Count == 0)
            itemMeta.Itemset.TextureNames.Add(itemName);
        else
            itemMeta.Itemset.TextureNames = Textures;
        
        return itemMeta;
    }
}