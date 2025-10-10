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
    }
}