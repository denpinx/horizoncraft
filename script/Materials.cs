using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
//using GodotDictionary = Godot.Collections.Dictionary;
using Dictionary = System.Collections.Generic.Dictionary<string, object>;
using System.Data.Common;
using System.Text.Json.Serialization;
using horizoncraft.script.Inventory;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script
{
    public class Materials
    {
        public static TileSet tileSet;
        public static List<BlockMeta> BlockMetas = new();
        public static List<ItemMeta> ItemMetas = new();
        public static Dictionary<string, ItemMeta> Dictionary_ItemMetas = new();

        public static Dictionary<string, BlockMeta> Dictionary_BlockMetas = new();
        public static List<EntityMeta> EntityMetas = new List<EntityMeta>();

        public static EntityMeta RegEntityMeta(EntityMeta meta)
        {
            meta.id = EntityMetas.Count;
            EntityMetas.Add(meta);
            return meta;
        }

        public static EntityMeta GetEntityMeta(String name)
        {
            foreach (EntityMeta em in EntityMetas)
            {
                if (em.NAME == name)
                {
                    return em;
                }
            }

            return null;
        }

        public static EntityMeta GetEntityMeta(int id)
        {
            foreach (EntityMeta em in EntityMetas)
            {
                if (em.id == id)
                {
                    return em;
                }
            }

            return null;
        }

        public static ItemMeta RegItemMeta(ItemMeta meta)
        {
            meta.Id = ItemMetas.Count;
            if (Dictionary_ItemMetas.ContainsKey(meta.Name))
            {
                GD.PrintErr($"{meta.Name} already exists");
                var blockmeta = Dictionary_ItemMetas[meta.Name].BlockMeta;
                Dictionary_ItemMetas[meta.Name] = meta;
                meta.BlockMeta = blockmeta;
                blockmeta.ItemMeta = meta;
            }
            else
            {
                ItemMetas.Add(meta);
                Dictionary_ItemMetas.Add(meta.Name, meta);
            }

            GD.Print($"[注册物品]{meta.Id} >{meta.Name}");
            return meta;
        }

        public static BlockMeta RegBlockMeta(BlockMeta meta)
        {
            meta.Id = BlockMetas.Count;
            BlockMetas.Add(meta);
            Dictionary_BlockMetas.Add(meta.Name, meta);
            GD.Print($"[注册方块]{meta.Id} >{meta.Name}");

            if (meta.Name != "air")
            {
                if (!Dictionary_ItemMetas.ContainsKey(meta.Name))
                {
                    ItemMeta itemMeta = new ItemMeta()
                    {
                        Name = meta.Name,
                        HasBlock = true
                    };
                    GD.Print($"[方块注册物品]{itemMeta.Name}");
                    itemMeta.Itemset.TextureNames.Add(meta.Name);
                    RegItemMeta(itemMeta);
                    itemMeta.BlockMeta = meta;
                    meta.ItemMeta = itemMeta;
                }
                else
                {
                    GD.Print($"[更新方块物品信息]{Dictionary_ItemMetas[meta.Name].Name}");
                    Dictionary_ItemMetas[meta.Name].BlockMeta = meta;
                    meta.ItemMeta = Dictionary_ItemMetas[meta.Name];
                }
            }

            return meta;
        }

        public static BlockMeta Valueof(string name)
        {
            return Dictionary_BlockMetas[name];
        }

        public static BlockMeta Valueof(int id)
        {
            if (id > BlockMetas.Count) GD.PrintErr($"[错误1] {id} BlockMeta 不存在！");
            return BlockMetas[id];
        }

        static Materials()
        {
            _ = LambdaCreater._factories;
            RegBlockMeta(
                new BlockMeta()
                {
                    Name = "air",
                    Id = -1,
                    Cube = false,
                    Collide = false
                }
            );

            LoadItemConfigs();
            LoadBlockConfigs();
            ProcessEntity();
            CreateTileSet();
            ProcessTextures();
        }

        private static void ProcessEntity()
        {
            RegEntityMeta(new EntityMeta("item_entity", "res://tscn/Entity/ItemEntity.tscn")
            {
                get_entity_node = (PackedScene packedScene) => (Node2D)packedScene.Instantiate<ItemEntity>()
            });
            RegEntityMeta(new EntityMeta("tree", "res://tscn/Entity/TreeEntity.tscn")
            {
                get_entity_node = (PackedScene packedScene) => (Node2D)packedScene.Instantiate<TreeEntity>()
            });
        }

        private static void LoadItemConfigs()
        {
            var fileAccess = FileAccess.Open("res://config/item/Materials.json", FileAccess.ModeFlags.Read);
            var jsonText = fileAccess.GetAsText();
            fileAccess.Close();
            var dict = JsonCleaner.FromJson(jsonText);
            foreach (string item_name in dict.Keys)
            {
                ItemMeta itemMeta = new ItemMeta()
                {
                    Name = item_name,
                };
                var item_dict = (Dictionary<string, object>)dict[item_name];
                if (item_dict.ContainsKey("components"))
                {
                    foreach (string cmp_name in ((Dictionary<string, object>)item_dict["components"]).Keys)
                    {
                        Dictionary<string, object> cmp_dict =
                            (Dictionary<string, object>)((Dictionary<string, object>)item_dict["components"])[cmp_name];
                        itemMeta.Components.Add(LambdaCreater.CreateLambda(cmp_name, cmp_dict));
                    }
                }
                if (item_dict.ContainsKey("tags"))
                {
                    var dict_attr = (Dictionary<string, object>)item_dict["tags"];
                    foreach (var v in dict_attr)
                        itemMeta.Tags.Add(v.Key, (string)v.Value);
                }

                if (item_dict.ContainsKey("max"))
                {
                    itemMeta.MaxAmount = (int)item_dict["max"];
                }

                if (item_dict.ContainsKey("state"))
                {
                    var itemset = new ItemStateSet();
                    var dict_state = (Dictionary<string, object>)item_dict["state"];
                    foreach (string state_name in dict_state.Keys)
                        itemset.TextureNames.Add(state_name);
                    itemMeta.Itemset = itemset;
                }
                else
                {
                    var itemset = new ItemStateSet()
                    {
                        TextureNames = new List<string>()
                        {
                            item_name
                        }
                    };
                    itemMeta.Itemset = itemset;
                }

                RegItemMeta(itemMeta);
            }
        }

        private static void LoadBlockConfigs()
        {
            FileAccess fileAccess = FileAccess.Open("res://config/block/Materials.json", FileAccess.ModeFlags.Read);
            string jsonText = fileAccess.GetAsText();
            fileAccess.Close();
            var dict = JsonCleaner.FromJson(jsonText);
            foreach (string item_name in dict.Keys)
            {
                Dictionary<string, object> config = (Dictionary<string, object>)dict[item_name];
                List<Func<Component>> components = new();
                if (config.ContainsKey("components"))
                {
                    foreach (string cmp_name in ((Dictionary<string, object>)config["components"]).Keys)
                    {
                        Dictionary<string, object> cmp_dict =
                            (Dictionary<string, object>)((Dictionary<string, object>)config["components"])[cmp_name];
                        components.Add(LambdaCreater.CreateLambda(cmp_name, cmp_dict));
                    }
                }

                var blockmeta = new BlockMeta()
                {
                    Name = item_name,
                    Components = components
                };
                if (config.ContainsKey("tiletype"))
                {
                    blockmeta.TileType = (string)config["tiletype"];
                }

                if (config.ContainsKey("light"))
                {
                    blockmeta.Light = (bool)config["light"];
                }

                if (config.ContainsKey("rigidity"))
                {
                    blockmeta.Rigidity = (float)Convert.ToDouble(config["rigidity"]);
                    GD.Print($"{blockmeta.Name} rgdt : {blockmeta.Rigidity}");
                }

                if (config.ContainsKey("tags"))
                {
                    var dict_attr = (Dictionary<string, object>)config["tags"];
                    foreach (var v in dict_attr)
                        blockmeta.Tags.Add(v.Key, (string)v.Value);
                }

                if (config.ContainsKey("mask"))
                {
                    var dict_mask = (Dictionary<string, object>)config["mask"];
                    if (dict_mask.ContainsKey("input"))
                    {
                        var list = (List<object>)dict_mask["input"];
                        foreach (var i in list)
                            blockmeta.InputMask.Add((int)i);
                    }

                    if (dict_mask.ContainsKey("output"))
                    {
                        var list = (List<object>)dict_mask["output"];
                        foreach (var i in list)
                            blockmeta.OutputMask.Add((int)i);
                    }
                }

                //配置不同状态下的Tile贴图
                if (config.ContainsKey("state"))
                {
                    List<BlockTileSet> blockTileSets = new List<BlockTileSet>();
                    Dictionary<string, object> state_dicts = (Dictionary<string, object>)config["state"];
                    int state_id = 0;
                    foreach (string state_name in state_dicts.Keys)
                    {
                        Dictionary<string, object> sdict = (Dictionary<string, object>)state_dicts[state_name];
                        var tile = new BlockTileSet()
                        {
                            state = state_id,
                        };

                        GD.Print($"创建贴图状态{state_name} -> {state_id}");
                        //定义了详细的名称就用定义的
                        if (sdict.ContainsKey("texture"))
                        {
                            tile.texture_name = (string)sdict["texture"];
                        }
                        //没有定义就用默认格式
                        else
                        {
                            tile.texture_name = $"{blockmeta.Name}_{state_name}";
                        }

                        if (sdict.ContainsKey("scene"))
                        {
                            tile.scene = (bool)sdict["scene"];
                            GD.Print($"{tile.texture_name} halight at {tile.scene}");
                        }

                        blockTileSets.Add(tile);
                        state_id++;
                    }

                    blockmeta.blockTileDatas = blockTileSets;
                }
                //没有配置State贴图，则默认加载此地址的贴图
                else
                {
                    blockmeta.blockTileDatas.Add(new BlockTileSet()
                    {
                        state = 0,
                        texture_name = blockmeta.Name
                    });
                }


                if (config.ContainsKey("cube")) blockmeta.Cube = (bool)config["cube"];
                if (config.ContainsKey("collide")) blockmeta.Collide = (bool)config["collide"];
                RegBlockMeta(blockmeta);
            }
        }

        public static void ProcessTextures()
        {
            var default_image = ResourceLoader.Load<Texture2D>(
                $"res://texture/item/default.png");

            for (int i = 0; i < ItemMetas.Count; i++)
            {
                ItemMeta meta = ItemMetas[i];
                for (int j = 0; j < meta.Itemset.TextureNames.Count; j++)
                {
                    var dir = $"res://texture/item/{meta.Itemset.TextureNames[j]}.png";
                    if (FileAccess.FileExists(dir))
                    {
                        var image = ResourceLoader.Load<Texture2D>(dir);
                        meta.Itemset.Textures.Add(j, image);
                    }
                    else
                    {
                        if (meta.HasBlock)
                        {
                            var block_dir = $"res://texture/block/{meta.Itemset.TextureNames[j]}.png";
                            if (FileAccess.FileExists(block_dir))
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

        public static TileSet CreateTileSet()
        {
            _ = BlockMetas;

            if (tileSet != null) return tileSet;
            else tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(16, 16);

            tileSet.AddPhysicsLayer();
            tileSet.AddOcclusionLayer();
            for (int i = 0; i < BlockMetas.Count; i++)
            {
                BlockMeta meta = BlockMetas[i];
                if (meta.Name == "air") continue;
                for (int state_index = 0; state_index < meta.blockTileDatas.Count; state_index++)
                {
                    BlockTileSet blockTileSet = meta.blockTileDatas[state_index];
                    if (blockTileSet.scene)
                    {
                        var ts = new TileSetScenesCollectionSource();
                        blockTileSet.tile_id = tileSet.AddSource(ts);
                        PackedScene ps = GD.Load<PackedScene>(blockTileSet.texture_name);
                        blockTileSet.id = ts.CreateSceneTile(ps);
                        blockTileSet.tile_count = 1;
                        blockTileSet.tile_size = 1;
                        GD.Print(
                            $"创建场景集合 {meta.Name},{blockTileSet.tile_id},{blockTileSet.scene} id:{blockTileSet.id}");
                    }
                    else
                    {
                        var image = ResourceLoader.Load<Texture2D>(
                            $"res://texture/block/{blockTileSet.texture_name}.png");
                        int tilesX = image.GetWidth() / 16;
                        int tilesY = image.GetHeight() / 16;
                        var atlasSource = new TileSetAtlasSource();
                        blockTileSet.tile_id = tileSet.AddSource(atlasSource);

                        atlasSource.Texture = image;
                        atlasSource.TextureRegionSize = new Vector2I(16, 16);
                        GD.Print($"创建图集{blockTileSet.tile_id}");
                        for (int y = 0; y < tilesY; y++)
                        for (int x = 0; x < tilesX; x++)
                        {
                            var id = new Vector2I(x, y);
                            atlasSource.CreateTile(id);
                            var tileData = atlasSource.GetTileData(id, 0);

                            if (meta.Collide)
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
                        }

                        blockTileSet.tile_size = tilesX;
                        blockTileSet.tile_count = tilesX * tilesY;
                    }
                }
            }

            return tileSet;
        }
    }
}