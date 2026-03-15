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
public class NeoMaterials(NeoLootTable lootTable)
{
    /// <summary>
    /// 可见的空气
    /// </summary>
    public bool VisibleAir = false;

    public TileSet BlockTileSets;
    private Dictionary<string, ItemMeta> ItemMetas = new();
    private Dictionary<string, BlockMeta> BlockMetas = new();

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

    protected ItemMeta RegItemMeta(ItemMeta itemMeta)
    {
        ItemMetas.Add(itemMeta.Name, itemMeta);
        GD.Print($"[NeoMaterials] Reg Block Meta: {itemMeta.Name,-16}, \t#{itemMeta.Id,-5}");
        return itemMeta;
    }

    protected BlockMeta RegBlockMeta(BlockMeta blockMeta)
    {
        if(!blockMeta.Tags.ContainsKey("thesaurus"))
            blockMeta.Tags.Add("thesaurus",blockMeta.Name);
        BlockMetas.Add(blockMeta.Name, blockMeta);
        GD.Print($"[NeoMaterials] Reg Block Meta: {blockMeta.Name,-16}, \t#{blockMeta.Id,-5}");
        if (blockMeta.Name == "air")
        {
            ItemMetas[blockMeta.Name].BlockMeta = blockMeta;
            blockMeta.ItemMeta = ItemMetas[blockMeta.Name];
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
        DirUtility.GetFiles("res://config/block",".json",block_json_paths);
        foreach (var block_json_path in block_json_paths)
        {
            FileAccess fileAccess = FileAccess.Open(block_json_path,FileAccess.ModeFlags.Read);
            string json = fileAccess.GetAsText();
            Dictionary<string,BlockMaterialsTemplate> blockMaterialsTemplates = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,BlockMaterialsTemplate>>(json);
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
}