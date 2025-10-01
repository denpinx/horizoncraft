using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.Expand;
using horizoncraft.script.Inventory;
using horizoncraft.script.Utility;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Struct;
using YamlDotNet;

namespace horizoncraft.script
{
    /// <summary>
    /// 更新:使用 tres作为配置文件，放弃json
    /// </summary>
    public class Materials
    {
        public static TileSet tileSet;
        public static List<BlockMeta> BlockMetas = new();
        public static List<ItemMeta> ItemMetas = new();

        public static Dictionary<string, ItemMeta> Dictionary_ItemMetas = new();
        public static Dictionary<string, BlockMeta> Dictionary_BlockMetas = new();

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


            if (Dictionary_ItemMetas.ContainsKey(meta.Name))
            {
                //覆盖更新
                var blockmeta = Dictionary_ItemMetas[meta.Name].BlockMeta;
                Dictionary_ItemMetas[meta.Name] = meta;
                meta.BlockMeta = blockmeta;
                blockmeta.ItemMeta = meta;
            }
            else
            {
                //添加
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

            if (meta.OreConfig != null)
            {
                OreManage.Registry(meta.OreConfig);
            }

            if (meta.Name != "air")
            {
                if (meta.IsLiquid)
                {
                    ItemMeta itemMeta = new ItemMeta()
                    {
                        Name = meta.Name + "_liquid",
                        MaxAmount = 1000
                    };

                    //TODO 待完成，还没想好用什么方案
                }

                if (!Dictionary_ItemMetas.ContainsKey(meta.Name))
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
                }
                else
                {
                    //更新
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
            if (id > BlockMetas.Count) GD.PrintErr($"{id} BlockMeta 不存在！");
            return BlockMetas[id];
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
                    Replaceable = true
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
            for (int i = 0; i < BlockMetas.Count; i++)
            {
                var meta = BlockMetas[i];
                meta.LootTable = new LootTable();
                ;
                foreach (var ls in meta._LootItemSnapshots_)
                {
                    var loot_item = new LootItem();
                    if (Dictionary_ItemMetas.TryGetValue(ls.Name, out var itemmeta))
                        loot_item.Item = itemmeta.GetItemStack();
                    loot_item.DropChance = ls.DropChance;
                    loot_item.AmountChances = ls.AmountChances;
                    loot_item.DropState = ls.DropState;
                    meta.LootTable.LootItems.Add(loot_item);
                    GD.Print($"[后处理] 添加战利品 {loot_item.Item.GetItemMeta().Name},战利品数{loot_item.AmountChances.Count}");
                }
            }
        }

        /// <summary>
        /// 加载所有物品配置
        /// </summary>
        private static void LoadAllItemConfigs()
        {
            var list = new List<string>();
            DirUtility.GetAllFiles("config/item", list);
            foreach (var fn in list)
            {
                if (!fn.EndsWith(".json")) continue;
                LoadItemConfigs(fn);
            }
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

        /// <summary>
        /// 加载所有方块配置
        /// </summary>
        [Obsolete]
        private static void LoadAllBlockConfigs()
        {
            var list = new List<string>();
            DirUtility.GetAllFiles("config/block", list);
            foreach (var fn in list)
            {
                if (!fn.EndsWith(".json")) continue;
                LoadBlockConfigs(fn);
            }
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
                if (config.ContainsKey("components"))
                {
                    foreach (string cmp_name in ((Dictionary<string, object>)config["components"]).Keys)
                    {
                        GD.Print("组件:" + cmp_name);
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

                foreach (var cmp in components)
                {
                    blockmeta.Examples.Add(cmp());
                }

                if (config.TryGetValue("liquid", out var value))
                {
                    blockmeta.IsLiquid = (bool)value;
                }

                if (config.TryGetValue("break-level", out var vbl))
                {
                    blockmeta.BreakLevel = (int)vbl;
                }

                if (config.ContainsKey("tiletype"))
                {
                    blockmeta.TileType = (string)config["tiletype"];
                }

                if (config.ContainsKey("light"))
                {
                    blockmeta.Light = (bool)config["light"];
                }

                if (config.ContainsKey("ore"))
                {
                    blockmeta.OreConfig = new OreConfig() { Name = blockmeta.Name };
                    var dict_ore = (Dictionary<string, object>)config["ore"];
                    if (dict_ore.ContainsKey("size")) blockmeta.OreConfig.Size = (int)dict_ore["size"];
                    if (dict_ore.ContainsKey("range")) blockmeta.OreConfig.Range = (int)dict_ore["range"];
                    if (dict_ore.ContainsKey("count")) blockmeta.OreConfig.Count = (int)dict_ore["count"];
                    if (dict_ore.ContainsKey("deep")) blockmeta.OreConfig.Deep = (int)dict_ore["deep"];
                }

                if (config.ContainsKey("rigidity"))
                {
                    blockmeta.Rigidity = (float)Convert.ToDouble(config["rigidity"]);
                }

                if (config.ContainsKey("replace"))
                {
                    blockmeta.Replaceable = (bool)config["replace"];
                }

                if (config.ContainsKey("tags"))
                {
                    var dict_attr = (Dictionary<string, object>)config["tags"];
                    foreach (var v in dict_attr)
                        blockmeta.Tags.Add(v.Key, (string)v.Value);
                }

                if (!blockmeta.Tags.ContainsKey("thesaurus"))
                {
                    blockmeta.Tags.Add("thesaurus", blockmeta.Name);
                }

                if (config.ContainsKey("loot"))
                {
                    var loot_list = (List<object>)config["loot"];
                    foreach (var v in loot_list)
                    {
                        var loot_dict = (Dictionary<string, object>)v;
                        var item = new LootItemSnapshot();

                        if (loot_dict.TryGetValue("drop-state", out var value1))
                            item.DropState = (int)value1;

                        if (loot_dict.ContainsKey("name")) item.Name = (string)loot_dict["name"];
                        else item.Name = blockmeta.Name;

                        if (loot_dict.ContainsKey("drop-chance"))
                        {
                            item.DropChance = (float)Convert.ToDouble(loot_dict["drop-chance"]);
                        }
                        else
                        {
                            item.DropChance = 1f;
                        }

                        if (loot_dict.ContainsKey("amount-chance"))
                        {
                            var amount_chance = (List<object>)loot_dict["amount-chance"];
                            foreach (var ac in amount_chance)
                            {
                                var acitem = (Dictionary<string, object>)ac;
                                var AmountChance = new AmountChance();
                                if (acitem.ContainsKey("amount")) AmountChance.Amount = (int)acitem["amount"];
                                else AmountChance.Amount = 1;

                                if (acitem.ContainsKey("chance"))
                                    AmountChance.Chance = (float)Convert.ToDouble(acitem["chance"]);
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

        /// <summary>
        /// 加载贴图
        /// </summary>
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

        /// <summary>
        /// 创建 TileSet
        /// </summary>
        /// <returns></returns>
        public static TileSet CreateTileSet()
        {
            _ = BlockMetas;

            if (tileSet != null) return tileSet;
            else tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(16, 16);

            tileSet.AddPhysicsLayer();
            tileSet.AddOcclusionLayer();
            tileSet.AddTerrainSet();

            tileSet.SetTerrainSetMode(0, TileSet.TerrainMode.Sides);
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
                        //
                        blockTileSet.tile_id = tileSet.AddSource(atlasSource);

                        atlasSource.Texture = image;
                        atlasSource.TextureRegionSize = new Vector2I(16, 16);
                        GD.Print($"创建图集{blockTileSet.tile_id}");

                        int mask = 0;
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