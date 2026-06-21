using System.Collections.Generic;
using Godot;

namespace Horizoncraft.script.Inventory;

public class ItemStateSet
{
    public List<string> TextureNames = new ();
    public Dictionary<int, Texture2D> Textures = new ();
}