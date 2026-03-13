using System.Text.Json.Serialization;

namespace Horizoncraft.script.WorldControl.Struct;

public class OreConfig
{
    [JsonPropertyName("vine-name")]
    public string Name = "coal_ore";
    /// <summary>
    /// 开始生成的高度条件
    /// </summary>
    [JsonPropertyName("deep")]
    public int Deep = 1;
    /// <summary>
    /// 生成次数
    /// </summary>
    [JsonPropertyName("count")]
    public int Count = 1;
    /// <summary>
    /// 生成最大数量
    /// </summary>
    [JsonPropertyName("size")]
    public int Size = 1;
    /// <summary>
    /// 生成最大范围
    /// </summary>
    [JsonPropertyName("range")]
    public int Range = 1;
}