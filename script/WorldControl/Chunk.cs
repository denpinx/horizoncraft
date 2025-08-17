using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
using Array = Godot.Collections.Array;
using MemoryPack;
using horizoncraft.script.Components;
using horizoncraft.script.Net;
using HorizonCraft.script.WorldControl.Service;

namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class Chunk:AsByteable<Chunk>
    {
        public int X;
        public int Y;
        public static int SizeZ = 2;
        public static int Size = 24;
        public bool spawn = false;
        public string BiomeType = "";

        [MemoryPackIgnore] public bool update_tilemap = true;
        [MemoryPackIgnore] public bool update_server = true;

        [MemoryPackIgnore]
        public Godot.Vector2I coord
        {
            get { return new(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

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
                update_tilemap = true;
                update_server = true;
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

        public void Tick(WorldBase WorldService, World world)
        {
            int state = 0;
            int id = 0;
            BlockTickEvent blockTickEvnet = new()
            {
                World = world,
                WorldService = WorldService,
                Chunk = this,
            };
            var coord = new Godot.Vector3I(0, 0, 0);
            //通过在循环不创建任何一个对象来优化
            for (int Z = 0; Z < Chunk.SizeZ; Z++)
            {
                for (int X = 0; X < Chunk.Size; X++)
                {
                    for (int Y = 0; Y < Chunk.Size; Y++)
                    {
                        if (data[X, Y, Z].components.Count != 0)
                        {
                            coord.X = this.coord.X * Chunk.Size + X;
                            coord.Y = this.coord.Y * Chunk.Size + Y;
                            coord.Z = Z;
                            state = data[X, Y, Z].STATE;
                            id = data[X, Y, Z].ID;
                            blockTickEvnet.Blockdata = data[X, Y, Z];
                            blockTickEvnet.GloablPos = coord;
                            coord.X = X;
                            coord.Y = Y;
                            coord.Z = Z;
                            blockTickEvnet.LocalPos = coord;

                            ComponentManager.ExecuteComponents(blockTickEvnet, data[X, Y, Z]);

                            if (state != data[X, Y, Z].STATE || id != data[X, Y, Z].ID)
                            {
                                update_tilemap = true;
                                update_server = true;
                            }
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