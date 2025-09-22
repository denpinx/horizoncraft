using System;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;

namespace horizoncraft.script.Expand;

public static class DictionaryExpand
{
    public static System.Collections.Generic.Dictionary<string, object> ToCsharp(
        this Dictionary<string, Variant> dict)
    {
        var result = new System.Collections.Generic.Dictionary<string, object>();
        foreach (var pair in dict)
        {
            var value = pair.Value.GetValue();
            result.Add(pair.Key, value);
        }
        return result;
    }
    public static System.Collections.Generic.Dictionary<string, string> ToCsharp(
        this Dictionary<string, string> dict)
    {
        var result = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var pair in dict)
        {
            var value = pair.Value;
            result.Add(pair.Key, value);
        }
        return result;
    }
    private static Object GetValue(this Variant variant)
    {
        switch (variant.VariantType)
        {
            case Variant.Type.Int:
                return variant.AsInt32();
            case Variant.Type.String:
                return variant.AsString();
            case Variant.Type.Float or Variant.Type.Signal:
                return variant.AsSingle();
            case Variant.Type.Bool:
                return variant.AsBool();
            default:
                return variant.ToString();
        }
    }
}