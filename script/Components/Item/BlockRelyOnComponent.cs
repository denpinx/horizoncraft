using MemoryPack;
namespace Horizoncraft.script.Components.Item;

/// <summary>
/// 玩家放置放置时会经过物品上的这个组件进行放置判断，决定要不要放置
/// 同时给予了一定的自定义配置
/// </summary>
[MemoryPackable]
public partial class BlockRelyOnComponent : ItemComponent
{
    public string RelyOnBlockName;
    public bool MatchTag = false;
    public int State = -1;
}