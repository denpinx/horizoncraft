using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Horizoncraft.script
{
    [Obsolete("待移除")]
    public class JsonCleaner
    {
        public static Dictionary<string, object> FromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return ConvertRoot(doc.RootElement) as Dictionary<string, object>;
        }
        public static object ConvertRoot(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ConvertObject(element),
                JsonValueKind.Array => ConvertArray(element),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number =>
                    element.TryGetInt32(out var i) ? (object)i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        private static Dictionary<string, object> ConvertObject(JsonElement obj)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in obj.EnumerateObject())
            {
                dict[prop.Name] = ConvertRoot(prop.Value);
            }

            return dict;
        }

        private static List<object> ConvertArray(JsonElement arr)
        {
            var list = new List<object>();
            foreach (var item in arr.EnumerateArray())
            {
                list.Add(ConvertRoot(item));
            }

            return list;
        }
    }
}