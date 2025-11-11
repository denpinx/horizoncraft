using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using MemoryPack;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Events;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Services.world;
using ReactiveComponent = Horizoncraft.script.Components.BlockComponents.ReactiveComponent;
using TickComponent = Horizoncraft.script.Components.BlockComponents.TickComponent;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Horizoncraft.script.WorldControl
{
    [MemoryPackable]
    public partial class Chunk
    {
        /// <summary>
        /// 因为异常原因导致的区块加载失败的标记
        /// </summary>
        // public bool Error = false;
        public static int SizeZ = 2;

        public static int Size = 50;
        public static int RandTickPerCount = Size / 2;
        public bool spawn = false;
        public long version;
        public int X;
        public int Y;
        public int[,] HighMap = new int[Size, SizeZ];
        public string BiomeType = "";
        //public string BiomeName = "";

        public int RemoveCount = 0;

        public HashSet<Vector3> TickList = new();
        public HashSet<Vector3> PassiveTickList = new();
        public List<Vector2> LightList = new();
        [MemoryPackIgnore] public int LightUpdateTime = 0;
        [MemoryPackIgnore] public List<Vector3I> UpdateList = new(32);
        [MemoryPackIgnore] public List<Vector3I> UpdateList_buffer = new();

        [MemoryPackIgnore] public bool update_tilemap = true;
        [MemoryPackIgnore] public bool update_server = true;

        [MemoryPackIgnore] public double TickUsedTime_μs;
        [MemoryPackIgnore] public Stopwatch _Stopwatch_tick_used = new Stopwatch();

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
        public List<EntityData> Entitys = new();

        public BlockData[,,] data = new BlockData[Size, Size, SizeZ];
        public double SpawnCostTime_μs;

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
                        data[X, Y, Z] = Materials.Valueof("air").CreateBlockData();
                    }
                }
            }
        }

        public BlockData GetBlock(int x, int y, int z)
        {
            return data[x, y, z];
        }

        public BlockData SetBlock(int x, int y, int z, BlockMeta meta, int state = 0)
        {
            var pos = new Vector3(x, y, z);
            var posv2 = new Vector2(x, y);
            if (meta.HasComponent<TickComponent>())
            {
                TickList.Add(pos);
            }
            else TickList.Remove(pos);

            if (z == 1)
            {
                if (meta.Light)
                {
                    if (!LightList.Contains(posv2))
                    {
                        LightList.Add(posv2);
                    }
                }
                else if (data[x, y, z].BlockMeta.Light)
                {
                    LightList.Remove(posv2);
                }
            }


            data[x, y, z].SetMeta(meta);
            data[x, y, z].State = state;
            UpdateList_buffer.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z));
            update_tilemap = true;
            return data[x, y, z];
        }

        public BlockData SetBlock(int x, int y, int z, BlockData blockData, int state = 0)
        {
            var pos = new Vector3(x, y, z);
            if (blockData.GetComponent<TickComponent>() != null)
            {
                TickList.Add(pos);
            }
            else TickList.Remove(pos);

            data[x, y, z].Components.Clear();
            data[x, y, z] = blockData;
            data[x, y, z].State = state;
            UpdateList_buffer.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z));
            update_tilemap = true;
            return data[x, y, z];
        }

        public void RandTick(WorldServiceBase WorldService, World world)
        {
            HashSet<Vector3I> randpos = new HashSet<Vector3I>(RandTickPerCount);
            for (int i = 0; i < RandTickPerCount;)
            {
                var pos = new Vector3I(Random.Shared.Next(0, Size), Random.Shared.Next(0, Size),
                    Random.Shared.Next(0, SizeZ));
                if (randpos.Add(pos))
                {
                    i++;
                }
            }

            BlockTickEvent randEvent = new()
            {
                World = world,
                Service = WorldService,
                Chunk = this,
            };
            foreach (var pos in randpos)
            {
                randEvent.LocalPos = pos;
                randEvent.GlobalePos = new Vector3I(
                    coord.X * Size + pos.X
                    , coord.Y * Size + pos.Y
                    , pos.Z);
                var block = GetBlock(pos.X, pos.Y, pos.Z);
                randEvent.BlockData = block;
                if (block?.Components == null) continue;

                var stateStart = block.State;

                ComponentManager.ExecuteRandBlockComponents(randEvent, block);

                if (stateStart != block.State)
                {
                    update_tilemap = true;
                    UpdateList_buffer.Add(randEvent.LocalPos);
                }

                randEvent.Reset();
            }
        }

        //50*50*2 2500个tick对象的情况下，平均每个区块最大耗时 1ms
        public void Tick(WorldServiceBase WorldService, World world)
        {
            _Stopwatch_tick_used.Restart();
            version = WorldService.TickTimes;
            
            UpdateList.Clear();
            for (int i = 0; i < UpdateList_buffer.Count; i++)
                UpdateList.Add(UpdateList_buffer[i]);
            UpdateList_buffer.Clear();


            BlockTickEvent blockTickEvnet = new()
            {
                World = world,
                Service = WorldService,
                Chunk = this,
            };
            var passiveticklist = PassiveTickList.ToArray();
            PassiveTickList.Clear();

            //TODO 同时拥有被动更新和主动更新会导致主动更新被更新两次
            foreach (var pos in passiveticklist)
            {
                var block = GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
                if (block?.Components == null) continue;
                foreach (var cmp in block.Components)
                {
                    var globale = new Vector3I((int)(this.coord.X * Chunk.Size + pos.X)
                        , (int)(this.coord.Y * Chunk.Size + pos.Y)
                        , (int)pos.Z);
                    if (cmp is ReactiveComponent)
                    {
                        blockTickEvnet.BlockData = block;
                        blockTickEvnet.GlobalePos = globale;
                        blockTickEvnet.LocalPos = pos.ToVector3I();
                        var state_start = block.State;
                        blockTickEvnet.Reset();
                        if (!ComponentManager.ExecuteBlockComponents(blockTickEvnet, block)) goto brek_out;
                        if (state_start != block.State)
                        {
                            update_tilemap = true;
                            UpdateList_buffer.Add(blockTickEvnet.LocalPos);
                        }
                    }
                }
                brek_out:
                {
                    update_tilemap = true;
                    UpdateList_buffer.Add(blockTickEvnet.LocalPos);
                }
            }


            var coord = new Godot.Vector3I(0, 0, 0);
            var local = new Godot.Vector3I(0, 0, 0);
            string id;
            int state;
            foreach (var item in TickList.ToArray())
                //for (int i = 0; i < TickList.Count; i++)
            {
                //var item = TickList;
                local.X = (int)item.X;
                local.Y = (int)item.Y;
                local.Z = (int)item.Z;
                coord.X = this.coord.X * Chunk.Size + (int)item.X;
                coord.Y = this.coord.Y * Chunk.Size + (int)item.Y;
                coord.Z = (int)item.Z;
                var block = GetBlock(local.X, local.Y, local.Z);
                if (block.Components.Count != 0)
                {
                    blockTickEvnet.BlockData = block;
                    blockTickEvnet.GlobalePos = coord;
                    blockTickEvnet.LocalPos = local;
                    id = block.Id;
                    state = block.State;
                    try
                    {
                        ComponentManager.ExecuteBlockComponents(blockTickEvnet, block);
                        blockTickEvnet.Reset();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    if (state != block.State || id != block.Id)
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

            RandTick(WorldService, world);

            _Stopwatch_tick_used.Stop();
            TickUsedTime_μs = _Stopwatch_tick_used.Elapsed.TotalMicroseconds;
        }


        public void SetLight(int light)
        {
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            {
                data[x, y, 1].OldLight = data[x, y, 1].Light;
                data[x, y, 1].Light = light;
                // if (!update_tilemap && data[x, y, 1].OldLight != light)
                // {
                //     LightUpdateTime++;
                //     update_tilemap = true;
                // }
            }
        }

        public bool CheckLightUpdate()
        {
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            {
                if (data[x, y, 1].OldLight != data[x, y, 1].Light)
                {
                    LightUpdateTime++;
                    update_tilemap = true;
                    return true;
                }
            }

            return false;
        }

        public HashSet<string> GetAllEntitys()
        {
            HashSet<string> result = new HashSet<string>();
            foreach (var entity in Entitys)
            {
                result.Add(entity.Name);
            }

            return result;
        }
    }
}