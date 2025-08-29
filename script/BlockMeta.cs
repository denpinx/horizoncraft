using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Events;
using horizoncraft.script.Inventory;
using horizoncraft.script.WorldControl;
using Dictionary = System.Collections.Generic.Dictionary<string, string>;

namespace horizoncraft.script
{
    public class BlockMeta
    {
        public Texture2D texture;
        public List<Func<Component>> Components = new();
        public Action<PlaceBlockEvent> PlaceBlockEvent;
        public Action<BlockTickEvent> BlockTickEvent;
        public List<BlockTileSet> blockTileDatas = new();
        public Dictionary Tags = new();
        public string Tiletype = "tile";
        public string NAME;
        public int SourceID;
        public int ID;
        public bool COLLIDE = true;
        public bool CUBE = true;
        public bool Light = false;
        public ItemMeta ItemMeta = null;
        public HashSet<int> InputMask = new();
        public HashSet<int> OutputMask = new();
        public float Rigidity = 0.5f;

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

        public string GetTag(string name)
        {
            if (Tags.ContainsKey(name)) return Tags[name];
            return null;
        }
    }
}