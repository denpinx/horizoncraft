using System.Text.Json;
using Godot;
using Godot.Collections;

namespace horizoncraft.script.I18N;

public class TextManage
{
    public string LanguageName = "";
    public string Description = "语言";
    public Dictionary<string, string> Texts = new Dictionary<string, string>();
    
    /// <summary>
    /// 如果存在，则返回翻译文本
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string Tr(string key)
    {
        if (Texts.TryGetValue(key, out string value))
            return value;
        GD.Print($"[TextManage] {key} 文本不存在");
        return key;
    }
}