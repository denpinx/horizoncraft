using System.Collections.Generic;
using Godot;

namespace horizoncraft.script.Inventory;

public class ItemStateSet
{
    public List<string> TextureNames =  new List<string>();
    public Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();
}