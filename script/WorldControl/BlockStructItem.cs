using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace horizoncraft.script.WorldControl
{
    public struct BlockStructItem
    {
        public Vector3I Coord;
        public BlockMeta BlockMeta;
        public int State;

        public bool ParseDictionary(System.Collections.Generic.Dictionary<string, object> dict)
        {
            if (Materials.BlockMetas.TryGetValue((string)dict["name"], out var meta))
            {
                BlockMeta = meta;
                Coord.X = (int)dict["x"];
                Coord.Y = (int)dict["y"];
                Coord.Z = (int)dict["z"];
                return true;
            }

            return false;
        }
    }
}