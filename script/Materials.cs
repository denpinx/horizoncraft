using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.RenderSystem;
using Horizoncraft.script.Utility;
using Horizoncraft.script.WorldControl;
using Horizoncraft.script.WorldControl.Struct;

namespace Horizoncraft.script
{
    /// <summary>
    /// 各种元数据
    /// </summary>
    public class Materials
    {
        public static TileSet tileSet;

        public static Dictionary<string, ItemMeta> ItemMetas = new();
        public static Dictionary<string, BlockMeta> BlockMetas = new();

        public static List<EntityMeta> EntityMetas = new();
        public static Dictionary<string, EntityMeta> DictionaryEntityMetas = new();

        public static EntityMeta RegEntityMeta(EntityMeta meta)
        {
            meta.Id = EntityMetas.Count;
            EntityMetas.Add(meta);
            DictionaryEntityMetas.Add(meta.Name, meta);
            return meta;
        }

        public static ItemMeta RegItemMeta(ItemMeta meta)
        {
            meta.Id = ItemMetas.Count;

            if (!meta.Tags.ContainsKey("thesaurus"))
                meta.Tags.Add("thesaurus", meta.Name);


            if (ItemMetas.ContainsKey(meta.Name))
            {
                //覆盖更新
                var blockmeta = ItemMetas[meta.Name].BlockMeta;
                ItemMetas[meta.Name] = meta;
                meta.BlockMeta = blockmeta;
                blockmeta.ItemMeta = meta;
            }
            else
            {
                //添加
                ItemMetas.Add(meta.Name, meta);
            }

            GD.Print($"[Materials] 注册物品 {meta.Name,-16} \t#{meta.Id,-5}");
            return meta;
        }

        public static BlockMeta RegBlockMeta(BlockMeta meta)
        {
            meta.Id = BlockMetas.Count;
            BlockMetas.Add(meta.Name, meta);
            GD.Print($"[Materials] 注册方块 {meta.Name,-16} \t#{meta.Id,-5}");

            if (meta.OreConfig != null)
            {
                OreManage.Registry(meta.OreConfig);
            }

            if (meta.Name != "air")
            {
                if (!ItemMetas.ContainsKey(meta.Name))
                {
                    //添加
                    ItemMeta itemMeta = new ItemMeta()
                    {
                        Name = meta.Name,
                        HasBlock = true
                    };
                    itemMeta.Itemset.TextureNames.Add(meta.Name);
                    itemMeta.Tags.Add("thesaurus", meta.Tags["thesaurus"]);
                    RegItemMeta(itemMeta);
                    itemMeta.BlockMeta = meta;
                    meta.ItemMeta = itemMeta;

                    foreach (var func in meta.Components.ToArray())
                    {
                        if (func() is ItemComponent)
                        {
                            itemMeta.AddItemComponentBuildFunc(func);
                            meta.Components.Remove(func);
                        }
                    }
                }
                else
                {
                    //更新
                    ItemMetas[meta.Name].BlockMeta = meta;
                    meta.ItemMeta = ItemMetas[meta.Name];

                    foreach (var func in meta.Components.ToArray())
                    {
                        if (func() is ItemComponent)
                        {
                            ItemMetas[meta.Name].AddItemComponentBuildFunc(func);
                            meta.Components.Remove(func);
                        }
                    }
                }
            }
            else
            {
                ItemMeta itemMeta = new ItemMeta()
                {
                    Name = meta.Name,
                    HasBlock = true
                };
                itemMeta.Itemset.TextureNames.Add(meta.Name);
                RegItemMeta(itemMeta);
                meta.ItemMeta = itemMeta;
            }

            return meta;
        }

        public static BlockMeta Valueof(string name)
        {
            return BlockMetas[name];
        }

        public static ItemMeta GetItemByTag(string tag)
        {
            foreach (var im in ItemMetas.Values)
            {
                if (im.GetTag("thesaurus") == tag)
                {
                    return im;
                }
            }

            return null;
        }

        static Materials()
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

            LoadAllItemConfigs();
            LoadAllBlockConfigs();
            ProcessEntity();
            CreateTileSet();
            ProcessTextures();
            Posttreatment();
        }

        private static void ProcessEntity()
        {
            RegEntityMeta(new EntityMeta("item_entity", "res://tscn/Entity/ItemEntity.tscn"));
        }

        /// <summary>
        /// 后处理
        /// </summary>
        private static void Posttreatment()
        {
            foreach (var meta in BlockMetas.Values)
            {
                meta.LootTable = new LootTable();
                ;
                foreach (var ls in meta._LootItemSnapshots_)
                {
                    var loot_item = new LootItem();
                    if (ItemMetas.TryGetValue(ls.Name, out var itemmeta))
                        loot_item.Item = itemmeta.GetItemStack();
                    loot_item.DropChance = ls.DropChance;
                    loot_item.AmountChances = ls.AmountChances;
                    loot_item.DropState = ls.DropState;
                    meta.LootTable.LootItems.Add(loot_item);
                }
            }
        }

        /// <summary>
        /// 加载所有物品配置
        /// </summary>
        private static void LoadAllItemConfigs()
        {
            var list = new List<string>();
            DirUtility.GetFiles("res://config/item", ".json", list);
            foreach (var fn in list)
                LoadItemConfigs(fn);
        }


        /// <summary>
        /// 加载指定地址的物品配置
        /// </summary>
        /// <param name="dir"></param>
        private static void LoadItemConfigs(string dir)
        {
            var fileAccess = FileAccess.Open(dir, FileAccess.ModeFlags.Read);
            var jsonText = fileAccess.GetAsText();
            fileAccess.Close();
            var dict = JsonCleaner.FromJson(jsonText);

            foreach (string item_name in dict.Keys)
            {
                if (item_name == "$schema") continue;

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

                        GD.Print("[Materials] 创建组件构造Lambda:" + cmp_name);
                        itemMeta.Components.Add(LambdaCreater.CreateLambda<Component>(cmp_name, cmp_dict));
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

        /// <summary>
        /// 加载所有方块配置
        /// </summary>
        private static void LoadAllBlockConfigs()
        {
            var list = new List<string>();
            DirUtility.GetFiles("res://config/block", ".json",list);
            foreach (var fn in list)
                LoadBlockConfigs(fn);
        }

        /// <summary>
        /// 加载指定的方块配置
        /// </summary>
        /// <param name="dir"></param>
        private static void LoadBlockConfigs(string dir)
        {
            FileAccess fileAccess = FileAccess.Open(dir, FileAccess.ModeFlags.Read);
            string jsonText = fileAccess.GetAsText();
            fileAccess.Close();
            var dict = JsonCleaner.FromJson(jsonText);
            foreach (string item_name in dict.Keys)
            {
                if (item_name == "$schema") continue;

                Dictionary<string, object> config = (Dictionary<string, object>)dict[item_name];
                List<Func<Component>> components = new();
                if (config.TryGetValue("components", out var componentsObject))
                {
                    foreach (string cmp_name in ((Dictionary<string, object>)componentsObject).Keys)
                    {
                        Dictionary<string, object> cmp_dict =
                            (Dictionary<string, object>)((Dictionary<string, object>)config["components"])[cmp_name];

                        GD.Print("[Materials] 创建组件构造Lambda:" + cmp_name);
                        components.Add(LambdaCreater.CreateLambda<Component>(cmp_name, cmp_dict));
                    }
                }

                var blockmeta = new BlockMeta()
                {
                    Name = item_name,
                    Components = components
                };

                foreach (var cmp in components)
                {
                    blockmeta.Examples.Add(cmp());
                }

                if (config.TryGetValue("liquid", out var value))
                {
                    blockmeta.IsLiquid = (bool)value;
                }

                if (config.TryGetValue("tile-visible", out var tile_visible_object))
                {
                    blockmeta.TileVisible = (bool)tile_visible_object;
                }

                if (config.TryGetValue("render", out var renderObject))
                {
                    var list = (List<object>)renderObject;
                    List<int> renderid = [];
                    foreach (var item in list)
                    {
                        int id = RenderSystemManager.GetRenderId((string)item);
                        if (id != -1)
                            renderid.Add(id);
                    }

                    blockmeta.RenderSystem = renderid;
                }

                if (config.TryGetValue("expand-texture", out var texture_object))
                {
                    var list = (List<object>)texture_object;
                    Dictionary<string, Texture2D> texture2Ds = [];
                    foreach (var item in list)
                    {
                        string texture_dir = (string)item;
                        var name = texture_dir
                            .Split('/')
                            .Last().Split(".png").First().ToLower();
                        var image = GD.Load<Texture2D>("res://texture/block/" + texture_dir);
                        texture2Ds.Add(name, image);
                    }

                    blockmeta.ExpandTextures = texture2Ds;
                }


                if (config.TryGetValue("break-level", out var vbl))
                {
                    blockmeta.BreakLevel = (int)vbl;
                }

                if (config.TryGetValue("tiletype", out var titletypeObject))
                {
                    blockmeta.TileType = (string)titletypeObject;
                }

                if (config.TryGetValue("light", out var lightOjbect))
                {
                    blockmeta.Light = (bool)lightOjbect;
                }

                if (config.TryGetValue("ore", out var oreObject))
                {
                    blockmeta.OreConfig = new OreConfig() { Name = blockmeta.Name };
                    var dict_ore = (Dictionary<string, object>)oreObject;
                    if (dict_ore.TryGetValue("size", out var size)) blockmeta.OreConfig.Size = (int)size;
                    if (dict_ore.TryGetValue("range", out var range)) blockmeta.OreConfig.Range = (int)range;
                    if (dict_ore.TryGetValue("count", out var count)) blockmeta.OreConfig.Count = (int)count;
                    if (dict_ore.TryGetValue("deep", out var deep)) blockmeta.OreConfig.Deep = (int)deep;
                }

                if (config.TryGetValue("rigidity", out var rigidityObject))
                {
                    blockmeta.Rigidity = (float)Convert.ToDouble(rigidityObject);
                }

                if (config.TryGetValue("replace", out var replaceObject))
                {
                    blockmeta.Replaceable = (bool)replaceObject;
                }

                if (config.TryGetValue("tags", out var tagsObject))
                {
                    var dict_attr = (Dictionary<string, object>)tagsObject;
                    foreach (var v in dict_attr)
                        blockmeta.Tags.Add(v.Key, (string)v.Value);
                }

                if (!blockmeta.Tags.ContainsKey("thesaurus"))
                {
                    blockmeta.Tags.Add("thesaurus", blockmeta.Name);
                }

                if (config.TryGetValue("loot", out var lootObject))
                {
                    var loot_list = (List<object>)lootObject;
                    foreach (var v in loot_list)
                    {
                        var loot_dict = (Dictionary<string, object>)v;
                        var item = new LootItemSnapshot();

                        if (loot_dict.TryGetValue("drop-state", out var value1))
                            item.DropState = (int)value1;

                        if (loot_dict.TryGetValue("name", out var nameObject)) item.Name = (string)nameObject;
                        else item.Name = blockmeta.Name;

                        if (loot_dict.TryGetValue("drop-chance", out var drop_chanceObject))
                        {
                            item.DropChance = (float)Convert.ToDouble(drop_chanceObject);
                        }
                        else
                        {
                            item.DropChance = 1f;
                        }

                        if (loot_dict.TryGetValue("amount-chance", out var amount_chanceObject))
                        {
                            var amount_chance = (List<object>)amount_chanceObject;
                            foreach (var ac in amount_chance)
                            {
                                var acitem = (Dictionary<string, object>)ac;
                                var AmountChance = new AmountChance();
                                if (acitem.TryGetValue("amount", out var amountOjbect))
                                    AmountChance.Amount = (int)amountOjbect;
                                else AmountChance.Amount = 1;

                                if (acitem.TryGetValue("chance", out var chanceObject))
                                    AmountChance.Chance = (float)Convert.ToDouble(chanceObject);
                                else AmountChance.Chance = 1;
                                item.AmountChances.Add(AmountChance);
                            }
                        }
                        else
                        {
                            var AmountChance = new AmountChance()
                            {
                                Amount = 1,
                                Chance = 1
                            };
                            item.AmountChances.Add(AmountChance);
                        }

                        blockmeta._LootItemSnapshots_.Add(item);
                    }
                }
                else
                {
                    var loot_item = new LootItemSnapshot()
                    {
                        Name = blockmeta.Name,
                        DropChance = 1f,
                        AmountChances = new List<AmountChance>()
                        {
                            new AmountChance()
                            {
                                Amount = 1,
                                Chance = 1
                            }
                        }
                    };
                    blockmeta._LootItemSnapshots_.Add(loot_item);
                }

                if (config.TryGetValue("mask", out var mask_object))
                {
                    var dict_mask = (Dictionary<string, object>)mask_object;
                    if (dict_mask.TryGetValue("input", out var input_object))
                    {
                        var list = (List<object>)input_object;
                        foreach (var i in list)
                            blockmeta.InputMask.Add((int)i);
                    }

                    if (dict_mask.TryGetValue("output", out var output_object))
                    {
                        var list = (List<object>)output_object;
                        foreach (var i in list)
                            blockmeta.OutputMask.Add((int)i);
                    }
                }

                if (config.TryGetValue("over-collide", out var list_objcet))
                {
                    var list = (List<object>)list_objcet;
                    List<OverCollideSet> overCollideSets = new();
                    ;
                    foreach (var dict_objcet in list)
                    {
                        var overCollide_dict = (Dictionary<string, object>)dict_objcet;
                        OverCollideSet overCollideSet = new();
                        overCollideSet.x = (int)overCollide_dict["x"];
                        overCollideSet.y = (int)overCollide_dict["y"];
                        overCollideSet.Collide = (bool)overCollide_dict["collide"];
                        overCollideSets.Add(overCollideSet);
                    }

                    blockmeta.overCollideDatas = overCollideSets;
                }

                //配置不同状态下的Tile贴图
                if (config.TryGetValue("state", out var state_objcet))
                {
                    List<BlockTileSet> blockTileSets = new List<BlockTileSet>();
                    Dictionary<string, object> state_dicts = (Dictionary<string, object>)state_objcet;
                    int state_id = 0;
                    foreach (string state_name in state_dicts.Keys)
                    {
                        Dictionary<string, object> sdict = (Dictionary<string, object>)state_dicts[state_name];
                        var tile = new BlockTileSet()
                        {
                            state = state_id,
                        };

                        //定义了详细的名称就用定义的
                        if (sdict.TryGetValue("texture", out var texture_objcet))
                        {
                            tile.texture_name = (string)texture_objcet;
                        }
                        //没有定义就用默认格式
                        else
                        {
                            tile.texture_name = $"{blockmeta.Name}_{state_name}";
                        }

                        if (sdict.TryGetValue("scene", out var scene_object))
                            tile.scene = (bool)scene_object;


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

        /// <summary>
        /// 加载贴图
        /// </summary>
        public static void ProcessTextures()
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

        /// <summary>
        /// 创建 TileSet
        /// </summary>
        /// <returns></returns>
        public static TileSet CreateTileSet(bool ShowAir = false)
        {
            _ = BlockMetas;

            if (tileSet != null) return tileSet;
            else tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(16, 16);

            tileSet.AddPhysicsLayer();
            tileSet.AddOcclusionLayer();
            tileSet.AddTerrainSet();

            tileSet.SetTerrainSetMode(0, TileSet.TerrainMode.Sides);
            foreach (var meta in BlockMetas.Values)
            {
                if (!ShowAir && meta.Name == "air") continue;

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
                        blockTileSet.tile_id = tileSet.AddSource(atlasSource);

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

            return tileSet;
        }
    }
}