using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Horizoncraft.script.Components.Interfaces;
using Horizoncraft.script.I18N;
using MemoryPack;

namespace Horizoncraft.script.Components.Item;

/// <summary>
/// 物品工具/耐久组件   
/// </summary>
[MemoryPackable]
public partial class ToolComponent : ItemComponent, ICopy, ITip
{
    public int Max;
    public int Value;
    public int ToolLevel;

    public int Efficiency;

    public List<string> Tag;

    public Component Copy()
    {
        return new ToolComponent()
        {
            Drive = this.Drive,
            Max = this.Max,
            ToolLevel = this.ToolLevel,
            Value = this.Value,
            Tag = this.Tag
        };
    }

    public string GetTip()
    {
        return
            "tip_item_tool_level".Trprefix("ui", ToolLevel) + "\n" +
            "tip_item_tool_type".Trprefix("ui", string.Join(',', Tag)) + "\n" +
            "tip_item_tool_durable".Trprefix("ui", Value, Max) + "\n";
    }
}