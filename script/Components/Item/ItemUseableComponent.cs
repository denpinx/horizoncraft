using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.Components.Item;

/// <summary>
/// 表示物品可以右键触发物品功能
/// 长按右键开始累加 UseTick
/// </summary>
[MemoryPackable]
public partial class ItemUsefulComponent : ItemComponent
{
    /// <summary>
    /// 右键时使用的总耗时
    /// </summary>
    public int UseTime;
}