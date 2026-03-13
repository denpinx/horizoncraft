using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Inventory;

namespace Horizoncraft.script;

/// <summary>
/// 新版本Materials，非静态版本，计划代替materials
/// </summary>
public class NeoMaterials
{
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

}