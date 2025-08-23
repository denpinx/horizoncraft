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
using Vector3 = System.Numerics.Vector3;

namespace horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class Chunk
    {
        public static int SizeZ = 2;
        public static int Size = 24;
        public bool spawn = false;
        public long version;

        public int X;
        public int Y;


        public string BiomeType = "";
        public string BiomeName = "";
        public List<Vector3> TickList = new();

        [MemoryPackIgnore] public List<Vector3I> UpdateList = new();
        [MemoryPackIgnore] public List<Vector3I> UpdateList_buffer = new();
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

        [MemoryPackConstructor]
        public Chunk()
        {
        }

        public Chunk(int x, int y)
        {
            this.X = x;
            this.Y = y;

            for (int X = 0; X < Chunk.Size; X++)
            {
                for (int Y = 0; Y < Chunk.Size; Y++)
                {
                    for (int Z = 0; Z < Chunk.SizeZ; Z++)
                    {
                        data[X, Y, Z] = Materials.Valueof("air").Blockdata();
                    }
                }
            }
        }

        public Blockdata GetBlock(int x, int y, int z)
        {
            return data[x, y, z];
        }

        public void SetBlock(int x, int y, int z, BlockMeta meta, int state = 0)
        {
            var pos = new Vector3(x, y, z);
            if (meta.Components.Count > 0)
            {
                if (!TickList.Contains(pos))
                {
                    TickList.Add(pos);
                }
            }
            else if (data[x, y, z].BlockMeta.Components.Count > 0)
                TickList.Remove(pos);


            data[x, y, z].SetMeta(meta);
            data[x, y, z].STATE = state;
            UpdateList_buffer.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z));
            update_tilemap = true;
        }

        public void SetBlock(int x, int y, int z, Blockdata blockdata, int state = 0)
        {
            var pos = new Vector3(x, y, z);
            if (blockdata.BlockMeta.Components.Count > 0)
            {
                if (!TickList.Contains(pos))
                {
                    TickList.Add(pos);
                }
            }
            else if (data[x, y, z].BlockMeta.Components.Count > 0)
                TickList.Remove(pos);

            data[x, y, z] = blockdata;
            data[x, y, z].STATE = state;
            UpdateList_buffer.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z));
            update_tilemap = true;
        }

        //新版，无法预期运行
        public void Tick(WorldBase WorldService, World world)
        {
            version = WorldService.TickTimes;
            UpdateList.Clear();
            if (UpdateList_buffer.Count > 0)
            {
                UpdateList.AddRange(UpdateList_buffer);
                UpdateList_buffer.Clear();
            }

            BlockTickEvent blockTickEvnet = new()
            {
                World = world,
                WorldService = WorldService,
                Chunk = this,
            };
            var coord = new Godot.Vector3I(0, 0, 0);
            var local = new Godot.Vector3I(0, 0, 0);
            int id;
            int state;
            // foreach (var item in TickList.ToArray())
            for (int i = 0; i < TickList.Count; i++)
            {
                var item = TickList[i];
                local.X = (int)item.X;
                local.Y = (int)item.Y;
                local.Z = (int)item.Z;
                coord.X = this.coord.X * Chunk.Size + (int)item.X;
                coord.Y = this.coord.Y * Chunk.Size + (int)item.Y;
                coord.Z = (int)item.Z;
                var block = GetBlock(local.X, local.Y, local.Z);
                if (block.components.Count != 0)
                {
                    blockTickEvnet.Blockdata = block;
                    blockTickEvnet.Globalpos = coord;
                    blockTickEvnet.LocalPos = local;
                    id = block.ID;
                    state = block.STATE;
                    ComponentManager.ExecuteComponents(blockTickEvnet, block);
                    if (state != block.STATE || id != block.ID)
                    {
                        update_tilemap = true;
                        UpdateList.Add(local);
                    }
                }
                else //异常方块重置组件
                if (block.BlockMeta.Components.Count != 0)
                {
                    block.SetMeta(block.BlockMeta);
                }
            }
        }

        // public void Tick(WorldBase WorldService, World world)
        // {
        //     version = WorldService.TickTimes;
        //
        //     UpdateList.Clear();
        //     if (UpdateList_buffer.Count > 0)
        //     {
        //         UpdateList.AddRange(UpdateList_buffer);
        //         UpdateList_buffer.Clear();
        //     }
        //
        //     BlockTickEvent blockTickEvnet = new()
        //     {
        //         World = world,
        //         WorldService = WorldService,
        //         Chunk = this,
        //     };
        //     var coord = new Godot.Vector3I(0, 0, 0);
        //     var coord_local = new Godot.Vector3I(0, 0, 0);
        //     int id = 0;
        //     int state = 0;
        //     //通过在循环不创建任何一个对象来优化
        //     for (byte Z = 0; Z < Chunk.SizeZ; Z++)
        //     {
        //         coord_local.Z = Z;
        //         coord.Z = Z;
        //         for (byte X = 0; X < Chunk.Size; X++)
        //         {
        //             coord_local.X = X;
        //             coord.X = this.coord.X * Chunk.Size + X;
        //             for (byte Y = 0; Y < Chunk.Size; Y++)
        //             {
        //                 coord_local.Y = Y;
        //                 coord.Y = this.coord.Y * Chunk.Size + Y;
        //                 var block = GetBlock(X, Y, Z);
        //                 if (block.components.Count != 0)
        //                 {
        //                     blockTickEvnet.Blockdata = block;
        //                     blockTickEvnet.GloablPos = coord;
        //                     blockTickEvnet.LocalPos = coord_local;
        //                     id = block.ID;
        //                     state = block.STATE;
        //                     ComponentManager.ExecuteComponents(blockTickEvnet, block);
        //                     if (state != block.STATE ||
        //                         id != block.ID)
        //                     {
        //                         update_tilemap = true;
        //                         //update_server = true;
        //
        //                         //增量更新数据
        //                         UpdateList.Add(new Vector3I(X, Y, Z));
        //                     }
        //                 }
        //                 else
        //                 {
        //                     //异常方块重置组件
        //                     if (block.BlockMeta.Components.Count != 0)
        //                     {
        //                         block.SetMeta(block.BlockMeta);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }
    }
}