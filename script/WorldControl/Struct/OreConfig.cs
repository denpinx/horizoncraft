using System.Threading;

namespace horizoncraft.script.WorldControl.Struct;

public class OreConfig
{
    public string Name = "coal_ore";
    /// <summary>
    /// 开始生成的高度条件
    /// </summary>
    public int Deep = 1;
    /// <summary>
    /// 生成次数
    /// </summary>
    public int Count = 1;
    /// <summary>
    /// 生成最大范围
    /// </summary>
    public int Size = 1;
}