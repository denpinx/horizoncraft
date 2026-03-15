using System.Text.Json.Serialization;

namespace Horizoncraft.script.WorldControl;

public class OverCollideSet
{
    [JsonPropertyName("x")] public int x { get; set; }
    [JsonPropertyName("y")] public int y { get; set; }
    [JsonPropertyName("collide")] public bool Collide { get; set; }
}