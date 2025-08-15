using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
using Array = Godot.Collections.Array;
using MemoryPack;
using horizoncraft.script.Components;
namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class Chunk
    {
        public int X;
        public int Y;
        public static int SizeZ = 2;
        public static int Size = 24;
        public bool update = true;
        public bool spawn = false;
        public string BiomeType = "";
        [MemoryPackIgnore]
        public Godot.Vector2I coord { get { return new(X, Y); } set { X = value.X; Y = value.Y; } }
        //加载完区块后释放到世界中，卸载区块时再从世界中捕获
        public List<Entitydata> entities = new();
        public Blockdata[,,] data = new Blockdata[Size, Size, SizeZ];
        public int SpawnCostTime;

        public int LoadCostTime;

        public Blockdata this[int x, int y, int z]
        {
            get { return data[x, y, z]; }
            set
            {
                update = true;
                data[x, y, z] = value;
            }
        }

        [MemoryPackConstructor]
        public Chunk()
        {

        }
        public Chunk(int x, int y)
        {
            coord = new Godot.Vector2I(x, y);
            for (int Z = 0; Z < Chunk.SizeZ; Z++)
            {
                for (int X = 0; X < Chunk.Size; X++)
                {
                    for (int Y = 0; Y < Chunk.Size; Y++)
                    {
                        data[X, Y, Z] = Materials.Valueof("air").Blockdata();
                    }
                }
            }
        }
        public void Tick(ChunkManageSql chunkManage)
        {
            int state = 0;
            for (int Z = 0; Z < Chunk.SizeZ; Z++)
            {
                for (int X = 0; X < Chunk.Size; X++)
                {
                    for (int Y = 0; Y < Chunk.Size; Y++)
                    {
                        if (data[X, Y, Z].components.Count != 0)
                        {
                            state = data[X, Y, Z].STATE;
                            BlockTickEvent blockTickEvnet = new()
                            {
                                World = chunkManage.world,
                                ChunkManageSql = chunkManage,
                                Chunk = this,
                                Blockdata = data[X, Y, Z],
                                GloablPos = new Godot.Vector3I(
                                    coord.X * Chunk.Size + X,
                                    coord.Y * Chunk.Size + Y,
                                    Z
                                ),
                                LocalPos = new Godot.Vector3I(X, Y, Z),
                            };
                            if (state != data[X, Y, Z].STATE) update = true;
                            ComponentManager.ExecuteComponents(blockTickEvnet, data[X, Y, Z]);
                        }
                        else
                        {
                            if (data[X, Y, Z].BlockMeta.Components.Count != 0)
                            {
                                data[X, Y, Z].SetMeta(data[X, Y, Z].BlockMeta);
                            }
                        }
                    }
                }
            }
        }
    }
}
