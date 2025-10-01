using System.Collections.Generic;
using Godot;
using horizoncraft.script.Utility;

namespace horizoncraft.script.I18N;

public static class LanguageManage
{
    public static string usingLang = "cn";
    private static Dictionary<string, TextManage> TextManages = new Dictionary<string, TextManage>();

    private static void AddText(string lang, string key, string value)
    {
        if (!TextManages.ContainsKey(lang))
        {
            TextManages.Add(lang, new TextManage());
        }

        GD.Print($"注册文本: {lang} - {key} : {value}");
        TextManages[lang].Texts[key] = value;
    }

    static LanguageManage()
    {
        List<string> files = new();
        DirUtility.GetAllFiles("config/lang", files);
        foreach (var file in files)
        {
            LoadLanguage(file);
        }
    }

    static void LoadLanguage(string dir)
    {
        FileAccess fileAccess = FileAccess.Open(dir, FileAccess.ModeFlags.Read);
        string jsonText = fileAccess.GetAsText();
        fileAccess.Close();
        var dict = JsonCleaner.FromJson(jsonText);
        if (dict.TryGetValue("lang", out var lang))
        {
            string perfix = "";
            if (dict.TryGetValue("prefix", out var perfix2))
            {
                perfix = perfix2 as string + ".";
            }


            if (dict.ContainsKey("texts"))
            {
                var dict_texts = (Dictionary<string, object>)dict["texts"];
                foreach (var kvp in dict_texts)
                {
                    AddText((string)lang, perfix + kvp.Key, kvp.Value as string);
                }
            }
        }
    }

    public static string Tr(this string key, params string[] args)
    {
        string result = key;
        if (TextManages.TryGetValue(usingLang, out var textManage))
        {
            result = textManage.Tr(key);

            if (args.Length > 0)
            {
                result = string.Format(result, args);
            }

            return result;
        }
        else GD.PrintErr($"[LanguageManage] {key} 文本不存在");
        return result;
    }

    /// <summary>
    /// 翻译
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="prefix">前缀</param>
    /// <returns></returns>
    public static string Trprefix(this string key, string prefix = "", params object[] paramss)
    {
        string result = key;
        if (prefix != "") result = prefix + "." + key;
        if (TextManages.TryGetValue(usingLang, out var textManage))
        {
            if (prefix != "")
            {
                result = textManage.Tr(prefix + "." + key);
            }
            else
            {
                result = textManage.Tr(key);
            }

            if (paramss.Length > 0)
            {
                result = string.Format(result, paramss);
            }

            return result;
        }

        if (prefix == "") GD.PrintErr($"[LanguageManage] {key} 文本不存在");
        else GD.PrintErr($"[LanguageManage] {prefix}.{key} 文本不存在");


        return result;
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    /// <param name="lang"></param>
    /// <param name="tree"></param>
    public static void SetLang(string lang, SceneTree tree)
    {
        if (TextManages.ContainsKey(lang))
        {
            usingLang = lang;
            ResetLang(tree.Root);
        }
        else
        {
            GD.PrintErr($"[LanguageManage] {lang} 语言选项不存在");
        }
    }

    /// <summary>
    /// 更新静态文本
    /// </summary>
    /// <param name="root"></param>
    private static void ResetLang(Node root)
    {
        foreach (var node in root.GetChildren())
        {
            if (node is ITranslatable itl)
            {
                itl.TranslateChange();
            }

            ResetLang(node);
        }
    }
}