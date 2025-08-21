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
        public static List<BlockMeta> blockmetas = new();
        public static List<ItemMeta> itemmetas = new();
        public static Dictionary<string, ItemMeta> Dictionary_itemmetas = new();


        public static Dictionary<string, BlockMeta> Dictionary_blockmetas = new();
        public static List<EntityMeta> entityMetas = new List<EntityMeta>();

        public static EntityMeta RegEntityMeta(EntityMeta meta)
        {
            meta.id = entityMetas.Count;
            entityMetas.Add(meta);
            return meta;
        }

        public static EntityMeta GetEntityMeta(String name)
        {
            foreach (EntityMeta em in entityMetas)
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
            foreach (EntityMeta em in entityMetas)
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
            meta.Id = itemmetas.Count;
            itemmetas.Add(meta);
            Dictionary_itemmetas.Add(meta.Name, meta);
            GD.Print($"[注册物品]{meta.Id} >{meta.Name}");
            return meta;
        }

        public static BlockMeta RegBlockMeta(BlockMeta meta)
        {
            meta.ID = blockmetas.Count;
            blockmetas.Add(meta);
            Dictionary_blockmetas.Add(meta.NAME, meta);
            GD.Print($"[注册方块]{meta.ID} >{meta.NAME}");
            if (!Dictionary_itemmetas.ContainsKey(meta.NAME) && meta.NAME != "air")
            {
                ItemMeta itemMeta = new ItemMeta()
                {
                    Name = meta.NAME,
                    HasBlock = true
                };
                itemMeta.Itemset.TextureNames.Add(meta.NAME);
                RegItemMeta(itemMeta);
            }

            return meta;
        }

        public static BlockMeta Valueof(string name)
        {
            return Dictionary_blockmetas[name];
        }

        public static BlockMeta Valueof(int id)
        {
            if (id > blockmetas.Count) GD.PrintErr($"[错误1] {id} BlockMeta 不存在！");
            return blockmetas[id];
        }

        static Materials()
        {
            _ = LambdaCreater._factories;
            RegBlockMeta(
                new BlockMeta()
                {
                    NAME = "air",
                    ID = -1,
                    CUBE = false,
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
                if (dict.ContainsKey("state"))
                {
                    var itemset = new ItemStateSet();
                    var dict_state = (Dictionary<string, object>)dict["state"];
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
                    NAME = item_name,
                    Components = components
                };
                if (config.ContainsKey("tiletype"))
                {
                    blockmeta.Tiletype = (string)config["tiletype"];
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
                        //定义了详细的名称就用定义的
                        if (sdict.ContainsKey("texture"))
                        {
                            tile.texture_name = (string)sdict["texture"];
                        }
                        //没有定义就用默认格式
                        else
                        {
                            tile.texture_name = $"{blockmeta.NAME}_{state_name}";
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
                        texture_name = blockmeta.NAME
                    });
                }


                if (config.ContainsKey("cube")) blockmeta.CUBE = (bool)config["cube"];
                if (config.ContainsKey("collide")) blockmeta.COLLIDE = (bool)config["collide"];
                RegBlockMeta(blockmeta);
            }
        }

        public static void ProcessTextures()
        {
            var default_image = ResourceLoader.Load<Texture2D>(
                $"res://texture/item/default.png");

            for (int i = 0; i < itemmetas.Count; i++)
            {
                ItemMeta meta = itemmetas[i];
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
                                meta.Itemset.Textures.Add(j, block_image);
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
            _ = blockmetas;

            if (tileSet != null) return tileSet;
            else tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(16, 16);

            tileSet.AddPhysicsLayer();
            tileSet.AddOcclusionLayer();
            for (int i = 0; i < blockmetas.Count; i++)
            {
                BlockMeta meta = blockmetas[i];
                if (meta.NAME == "air") continue;
                for (int state_index = 0; state_index < meta.blockTileDatas.Count; state_index++)
                {
                    BlockTileSet blockTileSet = meta.blockTileDatas[state_index];
                    var image = ResourceLoader.Load<Texture2D>($"res://texture/block/{blockTileSet.texture_name}.png");
                    int tilesX = image.GetWidth() / 16;
                    int tilesY = image.GetHeight() / 16;
                    var atlasSource = new TileSetAtlasSource();
                    atlasSource.Texture = image;
                    atlasSource.TextureRegionSize = new Vector2I(16, 16);
                    blockTileSet.tile_id = tileSet.AddSource(atlasSource);
                    for (int y = 0; y < tilesY; y++)
                    for (int x = 0; x < tilesX; x++)
                    {
                        var id = new Vector2I(x, y);
                        atlasSource.CreateTile(id);
                        var tileData = atlasSource.GetTileData(id, 0);

                        if (meta.COLLIDE)
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

            return tileSet;
        }
    }
}