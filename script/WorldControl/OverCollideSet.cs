using System.Text.Json.Serialization;

namespace Horizoncraft.script.WorldControl;

public class OverCollideSet
{
    [JsonPropertyName("x")]public int x;
    [JsonPropertyName("y")]public int y;
    [JsonPropertyName("collide")]public bool Collide;
}