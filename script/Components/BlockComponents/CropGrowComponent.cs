using System;
using MemoryPack;

namespace horizoncraft.script.Components.BlockComponents;

[MemoryPackable]
public partial class CropGrowComponent : TickComponent
{
    //是否必须含水
    public bool Water = true;
    
    //生成几率
    public Single GroupChance = 1.0f;

    //最大状态
    public int StateMax = 8;

    //生长总次数
    public int GrowthMax = 1;

    //生长次数
    public int GrowthValue = 0;
}