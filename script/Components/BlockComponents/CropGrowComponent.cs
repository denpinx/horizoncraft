using System;
using MemoryPack;

namespace Horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class CropGrowComponent : TickComponent
{
    //是否必须含水
    public bool Water = true;
    
    //每tick事件生长的几率
    public Single GrowChance = 1.0f;

    //生长的最大状态
    public byte GrowState = 8;

    //生长总次数，决定生长次数每满多少次就增长一次生长状态
    public byte GrowCount = 1;

    //生长次数
    public byte GrowValue = 0;
}