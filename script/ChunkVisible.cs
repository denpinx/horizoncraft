using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script
{
    public class ChunkVisible
    {
        public int Count;
        public Chunk CHUNK;
        public bool Visible = false;
        public TileMapLayerChunk tileMapLayerChunk = null;
    }
}
