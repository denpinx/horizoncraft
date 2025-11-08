using Horizoncraft.script.Components.Interfaces;
using Horizoncraft.script.I18N;
using MemoryPack;

namespace Horizoncraft.script.Components.Item;

/// <summary>
/// 物品工具/耐久组件   
/// </summary>
[MemoryPackable]
public partial class ItemDurableComponent : ItemComponent, ICopy, ITip
{
    public int Max;
    public int Value;

    public int ToolLevel;

    public int Efficiency;

    public string Tag;


    [MemoryPackIgnore] public string[] _tag_;

    public Component Copy()
    {
        return new ItemDurableComponent()
        {
            Name = this.Name,
            Max = this.Max,
            ToolLevel = this.ToolLevel,
            Value = this.Value,
            Tag = this.Tag
        };
    }

    public string[] GetTags()
    {
        if (_tag_ == null)
        {
            if (Tag == null) return ["any"];
            _tag_ = Tag.Contains('|') ? Tag.Split('|') : [Tag];
        }

        return _tag_;
    }

    public bool HasTag(string tag)
    {
        var strs = GetTags();
        if (strs.Length == 0) return false;
        foreach (var str in strs)
            if (str == tag)
                return true;
        return false;
    }

    public string GetTip()
    {
        return
            "tip_item_tool_level".Trprefix("ui", ToolLevel) + "\n" +
            "tip_item_tool_type".Trprefix("ui", Tag) + "\n" +
            "tip_item_tool_durable".Trprefix("ui", Value, Max) + "\n";
    }
}