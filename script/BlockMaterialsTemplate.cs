using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Horizoncraft.script;

/// <summary>
/// json 转 BlockMaterialsTemplate，然后构建成 BlockMeta
/// </summary>
public record BlockMaterialsTemplate
{
    [JsonPropertyName("max")] public int Max = 1;

    [JsonPropertyName("break-level")] public int BreakLevel = 0;

    [JsonPropertyName("liquid")] public bool Liquid = false;
    [JsonPropertyName("light")] public bool Light = false;
    [JsonPropertyName("tiletype")] public string? TileType;
    [JsonPropertyName("thesaurus")] public string? Thesaurus;
    [JsonPropertyName("rigidity")] public float Rigidity = 1;

    [JsonPropertyName("replace")] public bool Replace = false;

    [JsonPropertyName("mask")] public BlockMetaMaskTemplate Mask;

    [JsonPropertyName("tags")] public Dictionary<string, string> Tags = new();

    [JsonPropertyName("render")] public List<string> Render = new();

    [JsonPropertyName("expand-texture")] public Dictionary<string, string> ExpandTexture = new();
    [JsonPropertyName("state")] public Dictionary<string, BlockMetaStateTemplate> State = new();
    [JsonPropertyName("components")] public Dictionary<string, object> Components = new();

    public BlockMeta BuildBlockMeta()
    {
        return null;
    }
}

public record BlockMetaStateTemplate
{
    [JsonPropertyName("texture")] public string Texture;

    [JsonPropertyName("scene")] public bool Scene;
}

public record BlockMetaMaskTemplate
{
    [JsonPropertyName("input")] public List<int> Input;
    [JsonPropertyName("output")] public List<int> Output;
}