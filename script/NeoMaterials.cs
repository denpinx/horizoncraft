using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Utility;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script;

/// <summary>
/// 新版本Materials，非静态版本，计划代替materials
/// </summary>
[Obsolete("已禁用",true)]
public class NeoMaterials
{
    /// <summary>
    /// 可见的空气
    /// </summary>
    public bool VisibleAir = false;

    public TileSet BlockTileSets;
    public readonly Dictionary<string, ItemMeta> ItemMetas = new();
    public readonly Dictionary<string, BlockMeta> BlockMetas = new();

    private Dictionary<string, EntityMeta> EntityMetas = new();

    public ItemMeta GetItemMeta(string name)
    {
        return ItemMetas.ContainsKey(name) ? ItemMetas[name] : null;
    }

    public BlockMeta GetBlockMeta(string name)
    {
        return BlockMetas.ContainsKey(name) ? BlockMetas[name] : null;
    }

    public EntityMeta GetEntityMeta(string name)
    {
        return EntityMetas.ContainsKey(name) ? EntityMetas[name] : null;
    }

    public EntityMeta RegEntityMeta(EntityMeta meta)
    {
        meta.Id = EntityMetas.Count;
        EntityMetas.Add(meta.Name, meta);
        return meta;
    }

    protected ItemMeta RegItemMeta(ItemMeta itemMeta)
    {
        itemMeta.Id = ItemMetas.Count;
        if (!itemMeta.Tags.ContainsKey("thesaurus"))
            itemMeta.Tags.Add("thesaurus", itemMeta.Name);

        if (ItemMetas.ContainsKey(itemMeta.Name))
        {
            //覆盖更新
            var blockmeta = ItemMetas[itemMeta.Name].BlockMeta;
            ItemMetas[itemMeta.Name] = itemMeta;
            itemMeta.BlockMeta = blockmeta;
            blockmeta.ItemMeta = itemMeta;
        }
        else
        {
            //添加
            ItemMetas.Add(itemMeta.Name, itemMeta);
        }

        GD.Print($"[NeoMaterials] 注册物品: {itemMeta.Name,-16}, \t#{itemMeta.Id,-5}");
        return itemMeta;
    }

    protected BlockMeta RegBlockMeta(BlockMeta blockMeta)
    {
        blockMeta.Id = BlockMetas.Count;

        if (!blockMeta.Tags.ContainsKey("thesaurus"))
            blockMeta.Tags.Add("thesaurus", blockMeta.Name);
        BlockMetas.Add(blockMeta.Name, blockMeta);
        GD.Print($"[NeoMaterials] 注册方块: {blockMeta.Name,-16}, \t#{blockMeta.Id,-5}");
        if (blockMeta.Name == "air")
        {
            ItemMeta itemMeta = new ItemMeta()
            {
                Name = blockMeta.Name,
                HasBlock = true
            };
            itemMeta.Itemset.TextureNames.Add(blockMeta.Name);
            RegItemMeta(itemMeta);
            itemMeta.BlockMeta= blockMeta;
            blockMeta.ItemMeta = itemMeta;
        }
        else
        {
            if (ItemMetas.TryGetValue(blockMeta.Name, out var itemMeta))
            {
            }
            else
            {
                itemMeta = new ItemMeta()
                {
                    Name = blockMeta.Name,
                    HasBlock = true
                };
                itemMeta.Itemset.TextureNames.Add(blockMeta.Name);
                itemMeta.Tags.Add("thesaurus", blockMeta.Tags["thesaurus"]);
                RegItemMeta(itemMeta);
            }

            itemMeta.BlockMeta = blockMeta;
            blockMeta.ItemMeta = itemMeta;
            foreach (var func in blockMeta.Components.ToArray())
            {
                if (func() is ItemComponent)
                {
                    itemMeta.AddItemComponentBuildFunc(func);
                    blockMeta.Components.Remove(func);
                }
            }
        }

        return blockMeta;
    }

    public void LoadBlockMaterials()
    {
        var block_json_paths = new List<string>();
        DirUtility.GetFiles("res://config/block", ".json", block_json_paths);
        foreach (var block_json_path in block_json_paths)
        {
            FileAccess fileAccess = FileAccess.Open(block_json_path, FileAccess.ModeFlags.Read);
            string json = fileAccess.GetAsText();
            Dictionary<string, BlockMaterialsTemplate> blockMaterialsTemplates =
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, BlockMaterialsTemplate>>(json);
            int id = 0;
            foreach (var blockMaterialsTemplate in blockMaterialsTemplates)
            {
                var block_meta = blockMaterialsTemplate.Value.BuildBlockMeta(blockMaterialsTemplate.Key);
                block_meta.Id = id++;
                RegBlockMeta(block_meta);
            }
        }
    }

    public void LoadItemMaterials()
    {
        var block_json_paths = new List<string>();
        DirUtility.GetFiles("res://config/item", ".json", block_json_paths);
        foreach (var block_json_path in block_json_paths)
        {
            FileAccess fileAccess = FileAccess.Open(block_json_path, FileAccess.ModeFlags.Read);
            string json = fileAccess.GetAsText();
            Dictionary<string, ItemMaterialsTemplate> itemMaterialsTemplates =
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ItemMaterialsTemplate>>(json);
            int id = 0;
            foreach (var blockMaterialsTemplate in itemMaterialsTemplates)
            {
                var block_meta = blockMaterialsTemplate.Value.BuildItemMeta(blockMaterialsTemplate.Key);
                block_meta.Id = id++;
                RegItemMeta(block_meta);
            }
        }
    }

    public virtual void BuildTileSet()
    {
        BlockTileSets = new TileSet();
        BlockTileSets.TileSize = new Vector2I(16, 16);

        BlockTileSets.AddPhysicsLayer();
        BlockTileSets.AddOcclusionLayer();
        BlockTileSets.AddTerrainSet();

        BlockTileSets.SetTerrainSetMode(0, TileSet.TerrainMode.Sides);
        foreach (var meta in BlockMetas.Values)
        {
            if (!VisibleAir && meta.Name == "air") continue;

            for (int state_index = 0; state_index < meta.blockTileDatas.Count; state_index++)
            {
                BlockTileSet blockTileSet = meta.blockTileDatas[state_index];

                if (blockTileSet.scene)
                {
                    var ts = new TileSetScenesCollectionSource();
                    blockTileSet.tile_id = BlockTileSets.AddSource(ts);
                    PackedScene ps = GD.Load<PackedScene>(blockTileSet.texture_name);
                    blockTileSet.id = ts.CreateSceneTile(ps);
                    blockTileSet.tile_count = 1;
                    blockTileSet.tile_size = 1;
                }
                else
                {
                    var texture_name = "";
                    if (meta.TileVisible) texture_name = blockTileSet.texture_name;
                    else
                    {
                        meta.Texture = GD.Load<Texture2D>($"res://texture/block/{blockTileSet.texture_name}.png");
                        texture_name = "empty_block";
                    }

                    var image = GD.Load<Texture2D>(
                        $"res://texture/block/{texture_name}.png");

                    if (meta.Texture == null)
                        meta.Texture = image;


                    int tilesX = image.GetWidth() / 16;
                    int tilesY = image.GetHeight() / 16;
                    var atlasSource = new TileSetAtlasSource();
                    //
                    blockTileSet.tile_id = BlockTileSets.AddSource(atlasSource);

                    atlasSource.Texture = image;
                    atlasSource.TextureRegionSize = new Vector2I(16, 16);

                    int mask = 0;
                    for (int y = 0; y < tilesY; y++)
                    for (int x = 0; x < tilesX; x++)
                    {
                        var id = new Vector2I(x, y);
                        atlasSource.CreateTile(id);
                        var tileData = atlasSource.GetTileData(id, 0);
                        bool result = false;
                        var result_object = meta.overCollideDatas.Find(c => c.x == x && c.y == y);
                        if (result_object != null)
                        {
                            result = result_object.Collide;
                            if (!result) continue;
                        }


                        if (meta.Collide || result)
                        {
                            var half = 16 / 2.0f;
                            Vector2[] polygon = new Vector2[]
                            {
                                new Vector2(-half, -half),
                                new Vector2(half, -half),
                                new Vector2(half, half),
                                new Vector2(-half, half)
                            };
                            tileData.AddCollisionPolygon(0);
                            tileData.SetCollisionPolygonPoints(0, 0, polygon);
                            tileData.AddOccluderPolygon(0);
                            tileData.SetOccluderPolygon(0, 0, new OccluderPolygon2D()
                            {
                                Polygon = polygon
                            });
                        }

                        mask++;
                    }

                    blockTileSet.tile_size = tilesX;
                    blockTileSet.tile_count = tilesX * tilesY;
                }
            }
        }
    }

    public void ProcessEntity()
    {
        RegEntityMeta(new EntityMeta("item_entity", "res://tscn/Entity/ItemEntity.tscn"));
    }

    /// <summary>
    /// 加载贴图
    /// </summary>
    public void ProcessTextures()
    {
        var default_image = ResourceLoader.Load<Texture2D>(
            $"res://texture/item/default.png");
        
        foreach (var meta in ItemMetas.Values)
        {
            for (int j = 0; j < meta.Itemset.TextureNames.Count; j++)
            {
                var dir = $"res://texture/item/{meta.Itemset.TextureNames[j]}.png";

                if (ResourceLoader.Exists(dir))
                {
                    var image = ResourceLoader.Load<Texture2D>(dir);
                    meta.Itemset.Textures.Add(j, image);
                }
                else
                {
                    if (meta.HasBlock)
                    {
                        var block_dir = $"res://texture/block/{meta.Itemset.TextureNames[j]}.png";
                        if (ResourceLoader.Exists(block_dir))
                        {
                            var block_image = ResourceLoader.Load<Texture2D>(block_dir);
                            var wide = block_image.GetWidth();
                            var high = block_image.GetHeight();
                            meta.Itemset.Textures.Add(j, block_image);
                            if (wide != high)
                            {
                                var min = Mathf.Min(wide, high);
                                var img = block_image.GetImage();
                                img.Crop(min, min);
                                meta.ShowTexture = ImageTexture.CreateFromImage(img);
                            }
                        }
                        else
                            meta.Itemset.Textures.Add(j, default_image);
                    }
                    else
                        meta.Itemset.Textures.Add(j, default_image);
                }
            }
        }
    }

    public void Initialize()
    {
        RegBlockMeta(
            new BlockMeta()
            {
                Name = "air",
                Id = -1,
                Cube = false,
                Collide = false,
                Replaceable = true,
                blockTileDatas = new List<BlockTileSet>()
                {
                    new BlockTileSet()
                    {
                        texture_name = "air",
                        tile_size = 1,
                    }
                }
            }
        );
        
        LoadItemMaterials();
        LoadBlockMaterials();
        ProcessEntity();
        BuildTileSet();
        ProcessTextures();
    }
}