using System.ComponentModel.DataAnnotations;
using MemoryPack;

namespace horizoncraft.script.Components.Item;
/// <summary>
/// 物品工具/耐久组件
/// </summary>
[MemoryPackable]
public partial class ItemDurableComponent : ItemComponent
{
    public int Max;
    public int Value;

    public int ToolLevel;

    public int Efficiency;

    // List<string> Tag;
    // "ore|wood|others"
    public string Tag;


    [MemoryPackIgnore] public string[] _tag_;

    public override ItemComponent Copy()
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
}