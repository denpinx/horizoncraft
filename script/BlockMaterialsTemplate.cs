using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Godot;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.RenderSystem;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script;

/// <summary>
/// json 转 BlockMaterialsTemplate，然后构建成 BlockMeta
/// </summary>
public record BlockMaterialsTemplate
{
    [JsonPropertyName("max")] public int Max { set; get; } = 1;

    [JsonPropertyName("break-level")] public int BreakLevel { set; get; } = 0;

    [JsonPropertyName("liquid")] public bool Liquid { set; get; } = false;
    [JsonPropertyName("light")] public bool Light { set; get; } = false;
    [JsonPropertyName("cube")] public bool Cube { set; get; } = false;
    [JsonPropertyName("collide")] public bool Collide { set; get; } = false;
    [JsonPropertyName("tiletype")] public string? TileType { set; get; }
    [JsonPropertyName("thesaurus")] public string? Thesaurus { set; get; }
    [JsonPropertyName("rigidity")] public float Rigidity { set; get; } = 1;

    [JsonPropertyName("replace")] public bool Replace { set; get; } = false;

    [JsonPropertyName("mask")] public BlockMetaMaskTemplate Mask { set; get; }

    [JsonPropertyName("tags")] public Dictionary<string, string> Tags { set; get; } = new();

    [JsonPropertyName("render")] public List<string> Render { set; get; } = new();

    [JsonPropertyName("expand-texture")] public List<string> ExpandTexture { set; get; } = new();
    [JsonPropertyName("state")] public Dictionary<string, BlockMetaStateTemplate> State { set; get; } = new();
    [JsonPropertyName("components")] public Dictionary<string, object> Components { set; get; } = new();
    [JsonPropertyName("over-collide")] public List<OverCollideSet> OverCollides { set; get; } = new();
    [JsonPropertyName("loot-tabel-id")] public string LootTabelId { set; get; }

    public BlockMeta BuildBlockMeta(string blockName)
    {
        var blockMeta = new BlockMeta();
        blockMeta.Name = blockName;
        blockMeta.BreakLevel = BreakLevel;
        blockMeta.Cube = Cube;
        blockMeta.Collide = Collide;
        blockMeta.Light = Light;
        blockMeta.Rigidity = Rigidity;
        blockMeta.Replaceable = Replace;
        blockMeta.TileType = TileType;

        if (Mask != null)
        {
            blockMeta.InputMask = Mask.Input.ToHashSet();
            blockMeta.OutputMask = Mask.Output.ToHashSet();
        }


        foreach (var render_name in Render)
        {
            int id = RenderSystemManager.GetRenderId(render_name);
            if (id != -1) blockMeta.RenderSystem.Add(id);
        }

        foreach (var expandTexture in ExpandTexture)
        {
            var image_name = expandTexture.GetFile();
            var image = GD.Load<Texture2D>("res://texture/block/" + expandTexture);
            blockMeta.ExpandTextures.Add(image_name, image);
        }

        foreach (var overCollide in OverCollides)
        {
            blockMeta.overCollideDatas.Add(overCollide);
        }

        if (State.Count > 0)
        {
            int state_id = 0;
            foreach (var stateTemplate in State)
            {
                var tile = new BlockTileSet()
                {
                    texture_name = stateTemplate.Value.Texture,
                    scene = stateTemplate.Value.Scene,
                    state = state_id,
                };
                blockMeta.blockTileDatas.Add(tile);
            }
        }
        else
        {
            blockMeta.blockTileDatas.Add(new BlockTileSet()
            {
                state = 0,
                texture_name = blockMeta.Name
            });
        }

        if (LootTabelId != null)
            blockMeta.LootTableName = LootTabelId;
        else
            blockMeta.LootTableName = $"horizoncraft::block_meta::loot_table::{blockName}";
        return blockMeta;
    }
}

public record BlockMetaStateTemplate
{
    [JsonPropertyName("texture")] public string Texture { set; get; }

    [JsonPropertyName("scene")] public bool Scene { set; get; }
}

public record BlockMetaMaskTemplate
{
    [JsonPropertyName("input")] public List<int> Input { set; get; }
    [JsonPropertyName("output")] public List<int> Output { set; get; }
}