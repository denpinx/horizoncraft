using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.Components;
using horizoncraft.script.Events;
using horizoncraft.script.WorldControl;
namespace horizoncraft.script
{
    public class BlockMeta
    {
        public Texture2D texture;
        public List<Func<Component>> Components = new();
        public Action<PlaceBlockEvent> PlaceBlockEvent;
        public Action<BlockTickEvent> BlockTickEvent;
        public List<BlockTileSet> blockTileDatas = new();
        public string Tiletype = "tile";
        public string NAME;
        public int SourceID;
        public int ID;
        public bool COLLIDE = true;
        public bool CUBE = true;

        public Blockdata Blockdata()
        {
            return new Blockdata(this);
        }

        public BlockTileSet GetBlockTileSet(int state)
        {
            for (int i = 0; i < blockTileDatas.Count; i++)
            {
                if (blockTileDatas[i].state == state) return blockTileDatas[i];
            }
            if (blockTileDatas.Count == 1) return blockTileDatas[0];
            else return null;
        }
    }
}
