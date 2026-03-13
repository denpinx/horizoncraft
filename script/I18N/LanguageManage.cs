using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Utility;

namespace Horizoncraft.script.I18N;

/// <summary>
/// 本地化管理器，通常情况下，只会加载使用时的语言
/// </summary>
public static class LanguageManage
{
    public static string CurrentLanguage = "cn";
    public static List<string> Langs = new List<string>();
    public static Dictionary<string, TextManage> TextManages = new Dictionary<string, TextManage>();

    private static void AddText(string lang, string key, string value)
    {
        if (!TextManages.ContainsKey(lang))
        {
            TextManages.Add(lang, new TextManage());
        }

        TextManages[lang].Texts[key] = value;
    }

    static LanguageManage()
    {
        ReLoadTargetLanguage(CurrentLanguage);
    }

    private static void ReLoadTargetLanguage(string Lang)
    {
        TextManages.Clear();

        List<string> files = new();
        
        DirUtility.GetFiles("res://config/lang", ".json",files);
        foreach (var file in files)
        {
            LoadTargetLanguage(file, Lang);
        }
    }

    /// <summary>
    /// 加载目标语言
    /// </summary>
    /// <param name="dir">目录</param>
    /// <param name="targetlang">目标语言</param>
    private static void LoadTargetLanguage(string dir, string targetlang)
    {
        FileAccess fileAccess = FileAccess.Open(dir, FileAccess.ModeFlags.Read);
        string jsonText = fileAccess.GetAsText();
        fileAccess.Close();
        var dict = JsonCleaner.FromJson(jsonText);
        if (dict.TryGetValue("lang", out var lang))
        {
            if (!Langs.Contains((string)lang))
            {
                Langs.Add((string)lang);
            }

            if ((string)lang != targetlang) return;
            string perfix = "";
            if (dict.TryGetValue("prefix", out var perfix2))
            {
                perfix = perfix2 as string + ".";
            }


            if (dict.TryGetValue("texts", out var value))
            {
                var dict_texts = (Dictionary<string, object>)value;
                foreach (var kvp in dict_texts)
                {
                    AddText((string)lang, perfix + kvp.Key, kvp.Value as string);
                }
            }
        }
    }

    /// <summary>
    /// 本地化文本
    /// </summary>
    /// <param name="key"></param>
    /// <param name="args">文本格式化参数　用　｛０｝｛１｝表示</param>
    /// <returns></returns>
    public static string Tr(this string key, params object[] args)
    {
        string result = key;
        if (TextManages.TryGetValue(CurrentLanguage, out var textManage))
        {
            result = textManage.Tr(key);

            if (args.Length > 0)
            {
                result = string.Format(result, args);
            }
        }
        else GD.PrintErr($"[LanguageManage] {key} 文本不存在");

        return result;
    }

    /// <summary>
    /// 本地化文本,且使用前缀
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="prefix">前缀</param>
    /// <returns></returns>
    public static string Trprefix(this string key, string prefix = "", params object[] args)
    {
        string result = key;
        if (prefix != "") result = prefix + "." + key;
        if (TextManages.TryGetValue(CurrentLanguage, out var textManage))
        {
            if (prefix != "")
            {
                result = textManage.Tr(prefix + "." + key);
            }
            else
            {
                result = textManage.Tr(key);
            }

            if (args.Length > 0)
            {
                result = string.Format(result, args);
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
    /// <param name="lang">目标语言</param>
    /// <param name="tree">场景树</param>
    public static void SetTargetLang(string lang, SceneTree tree)
    {
        ReLoadTargetLanguage(lang);

        if (TextManages.ContainsKey(lang))
        {
            CurrentLanguage = lang;
            ResetLang(tree.Root);
            //更新ui管理器中的未场景树被托管的节点
            foreach (var invs in InventoryManage.Inventorys.Values)
            {
                ResetLang(invs);
            }
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
                if (node.IsNodeReady())
                    itl.TranslateChange();
                //延迟到场景加载完成时再更新
                else node.Ready += () => itl.TranslateChange();
            }

            ResetLang(node);
        }
    }
}