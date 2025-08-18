using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
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
    public partial class Chunk : AsByteable<Chunk>
    {
        public int X;
        public int Y;
        public static int SizeZ = 2;
        public static int Size = 24;
        public bool spawn = false;

        public string BiomeType = "";

        //对外只读
        [MemoryPackIgnore] public ChunkUpdataPack pack = new();

        //外部通过这个修改
        [MemoryPackIgnore] public ChunkUpdataPack pack_buffer = new();
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

        public BlockSnapShotItem[,,] Copy()
        {
            BlockSnapShotItem[,,] blocks = new BlockSnapShotItem[Chunk.Size, Chunk.Size, Chunk.SizeZ];
            for (int Z = 0; Z < Chunk.SizeZ; Z++)
            {
                for (int X = 0; X < Chunk.Size; X++)
                {
                    for (int Y = 0; Y < Chunk.Size; Y++)
                    {
                        blocks[X, Y, Z] = new BlockSnapShotItem()
                        {
                            id = (short)data[X, Y, Z].ID,
                            state = (byte)data[X, Y, Z].STATE
                        };
                    }
                }
            }

            return blocks;
        }

        public void Tick(WorldBase WorldService, World world)
        {
            pack.updates.Clear();
            if (pack_buffer.updates.Count > 0)
            {
                pack.updates.AddRange(pack_buffer.updates);
                pack_buffer.updates.Clear();
            }

            BlockSnapShotItem[,,] blockSnapShotItem = Copy();
            BlockTickEvent blockTickEvnet = new()
            {
                World = world,
                WorldService = WorldService,
                Chunk = this,
            };
            var coord = new Godot.Vector3I(0, 0, 0);
            var coord_local = new Godot.Vector3I(0, 0, 0);
            //通过在循环不创建任何一个对象来优化
            for (byte Z = 0; Z < Chunk.SizeZ; Z++)
            {
                coord_local.Z = Z;
                coord.Z = Z;
                for (byte X = 0; X < Chunk.Size; X++)
                {
                    coord_local.X = X;
                    coord.X = this.coord.X * Chunk.Size + X;
                    for (byte Y = 0; Y < Chunk.Size; Y++)
                    {
                        coord_local.Y = Y;
                        coord.Y = this.coord.Y * Chunk.Size + Y;
                        if (data[X, Y, Z].components.Count != 0)
                        {
                            blockTickEvnet.Blockdata = data[X, Y, Z];
                            blockTickEvnet.GloablPos = coord;
                            blockTickEvnet.LocalPos = coord_local;
                            ComponentManager.ExecuteComponents(blockTickEvnet, data[X, Y, Z]);
                            if (blockSnapShotItem[X, Y, Z].state != data[X, Y, Z].STATE ||
                                blockSnapShotItem[X, Y, Z].id != data[X, Y, Z].ID)
                            {
                                update_tilemap = true;
                                //update_server = true;

                                //增量更新数据
                                pack.updates.Add(new BlockSnapshot()
                                {
                                    x = X,
                                    y = Y,
                                    z = Z,
                                    id = (byte)data[X, Y, Z].ID,
                                    state = (byte)data[X, Y, Z].STATE,
                                });
                            }
                        }
                        else
                        {
                            //异常方块重置组件
                            if (data[X, Y, Z].BlockMeta.Components.Count != 0)
                            {
                                data[X, Y, Z].SetMeta(data[X, Y, Z].BlockMeta);
                            }
                        }
                    }
                }
            }

            //第二次判断，有组件在运行时修改了其他方块的值
            if (pack_buffer.updates.Count > 0)
            {
                pack.updates.AddRange(pack_buffer.updates);
                pack_buffer.updates.Clear();
            }

            pack.x = X;
            pack.y = Y;
        }
    }
}