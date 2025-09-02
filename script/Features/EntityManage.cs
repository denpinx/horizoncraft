using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.Components;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Entity;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;


namespace horizoncraft.script.Features
{
    /// <summary>
    /// 原型测试版本
    /// 待移除,调用的函数还没有被清除,先占个位置
    /// </summary>
    public class EntityManage
    {
        // public static bool Enable = false;
        // static WorldBase _worldManage;
        // public static ConcurrentDictionary<string, IEntityNode> waitEntitys = new();
        // public static System.Collections.Generic.Dictionary<string, IEntityNode> entitys = new();

        static EntityManage()
        {
        }

        public static void Init(WorldBase world)
        {
            // if (Enable)
            // {
            //     waitEntitys.Clear();
            //     entitys.Clear();
            //     GD.Print("切换场景，重新订阅");
            // }
            // else
            // {
            //     Player.GetInformation.Add(() => $"已加载生物：{entitys.Count}");
            //     Player.GetInformation.Add(() => $"待加载生物：{waitEntitys.Count}");
            //     Player.GetInformation.Add(() => $"当前场景类型：{_worldManage.GetType().ToString()}");
            // }
            //
            // Enable = true;
            //_worldManage = world;
            //_worldManage.OnChunkLoaded += OnWorldLoaded;
            //_worldManage.OnChunkUnLoading += OnWorldUnLoading;
            //_worldManage.world.CilentTicked += CilentTicked;
            //chunkManageSql.world.TileMapRemove += GetEntity;
        }

        public static void RemoveAtUUid(string uuid)
        {
            // if (entitys.Remove(uuid, out var entity))
            // {
            //     entity.GetNode().QueueFree();
            // }
        }

        public static IEntityNode CreateEntity(EntityComponent data, string uuid = null)
        {
            // GD.Print($"{_worldManage.TickTimes} - [创建实体]");
            // uuid ??= Guid.NewGuid().ToString();
            // if (data.Uuid == "") data.Uuid = uuid;
            // //该实体属于最先创建的玩家
            // //这里是本地的玩家名，这个方法服务端和客户端都有，如果是服务端，就是服务端的主机玩家所属
            // if (data.Belong == "") data.Belong = Player.Profile.Name;
            // EntityMeta entityMeta = Materials.GetEntityMeta(data.Id);
            //
            // if (entityMeta != null)
            // {
            //     IEntityNode entity = entityMeta.GetEntityNode();
            //     entity.World = _worldManage.world;
            //     entity.Component = data;
            //     waitEntitys.TryAdd(data.Uuid, entity);
            //     return entity;
            // }
            //
            // GD.PrintErr($"{_worldManage.TickTimes} - [实体不存在]！{data.Id},{data.Name}");
            return null;
        }

        public static IEntityNode CreateEntitybyName(string name, EntityComponent data)
        {
            // GD.Print($"{_worldManage.TickTimes} - [创建实体,基于名称]");
            // data.Uuid = Guid.NewGuid().ToString();
            // if (data.Belong == "") data.Belong = Player.Profile.Name;
            // EntityMeta entityMeta = Materials.GetEntityMeta(name);
            // if (entityMeta != null)
            // {
            //     IEntityNode entity = entityMeta.GetEntityNode();
            //     data.Id = entity.Id;
            //     entity.World = _worldManage.world;
            //     entity.Component = data;
            //     waitEntitys.TryAdd(data.Uuid, entity);
            //     return entity;
            // }
            //
            // GD.PrintErr($"{_worldManage.TickTimes} - [实体不存在]！{data.Id},{data.Name}");
            return null;
        }

        public static void OnWorldLoaded(WorldBase worldManage, Chunk chunk)
        {
            // foreach (EntityComponent data in chunk.entities)
            // {
            //     var entity = CreateEntity(data);
            //     waitEntitys.TryAdd(data.Uuid, entity);
            // }
            //
            // if (chunk.entities.Count > 0) GD.Print($"{_worldManage.TickTimes} - [加载实体] {chunk.entities.Count}");
            // chunk.entities.Clear();
        }

        public static void OnWorldUnLoading(WorldBase worldManage, Chunk chunk)
        {
            // if (worldManage is WorldClientService)
            // {
            //     var uuids = GetEntity(chunk, true);
            //     if (uuids.Count > 0)
            //     {
            //         UUIDPack pack = new()
            //         {
            //             uuids = uuids
            //         };
            //         GD.Print($"上传客户端实体到服务端:{uuids.Count}");
            //         worldManage.world.RpcId(1, "ResetEntityBelong", ByteTool.ToBytes(pack));
            //     }
            // }
            // else
            // {
            //     GetEntity(chunk);
            // }
        }

        ///释放区块的实体数据到实体管理器
        public static void DisposChunkEntity(Chunk chunk)
        {
            // if (chunk.entities.Count < 0) return;
            // for (int i = 0; i < chunk.entities.Count; i++)
            // {
            //     GD.Print($"释放区块实体:{chunk.entities[i]}");
            //     var cmp = chunk.entities[i] as EntityComponent;
            //     var entity = CreateEntity(cmp);
            //     waitEntitys.TryAdd(cmp.Uuid, entity);
            // }
        }

        /// <summary>
        /// 抓取实体到区块内
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="reset"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static List<string> GetEntity(Chunk chunk, bool reset = false, bool take = true)
        {
            // List<string> uuids = new List<string>();
            // GD.Print($"[EntityManage] {chunk.coord}");
            // foreach (var uuid in entitys.Keys.ToArray())
            // {
            //     IEntityNode entity = entitys[uuid];
            //
            //     if (entity.Component is EntityComponent)
            //     {
            //         GD.Print("is EntityComponent");
            //     }
            //     else
            //     {
            //         GD.Print($"not EntityComponent at {uuid}");
            //     }
            //     
            //     var cmp = entity.Component as EntityComponent;
            //     
            //     
            //     if (cmp.ChunkCoord == chunk.coord)
            //     {
            //     chunk.entities.Add(entity.Component);
            //     _worldManage.world.RemoveChild(entity.GetNode());
            //     if (reset)
            //     {
            //         if (cmp.Belong == Player.Profile.Name)
            //         {
            //             uuids.Add(cmp.Uuid);
            //         }
            //     }
            //
            //     if (take && entitys.Remove(uuid, out _))
            //         entity.GetNode().QueueFree();
            //     // }
            // }
            //
            // //删除待加载的实体
            // foreach (var uuid in waitEntitys.Keys.ToArray())
            // {
            //     var entity = waitEntitys[uuid];
            //     if (entity.Component is EntityComponent cmp && cmp.ChunkCoord == chunk.coord)
            //     {
            //         chunk.entities.Add(entity.Component);
            //
            //         if (take && waitEntitys.Remove(uuid, out _)) entity.GetNode().QueueFree();
            //     }
            // }
            //
            // if (chunk.entities.Count > 0) GD.Print("保存实体：" + chunk.entities.Count);
            // return uuids;
            return null;
        }

        public static List<Component> GetMovedEntity(Vector2I coord)
        {
            // List<Component> result = new();
            // foreach (var uuid in entitys.Keys.ToArray())
            // {
            //     IEntityNode entity = entitys[uuid];
            //     if (entity.Component is EntityComponent cmp && cmp.ChunkCoord == coord)
            //     {
            //         if (cmp.Update)
            //         {
            //             result.Add(cmp);
            //         }
            //     }
            // }
            //
            // foreach (var uuid in waitEntitys.Keys.ToArray())
            // {
            //     IEntityNode entity = waitEntitys[uuid];
            //     if (entity.Component is EntityComponent cmp && cmp.ChunkCoord == coord)
            //     {
            //         if (cmp.Update)
            //         {
            //             result.Add(cmp);
            //         }
            //     }
            // }

            //return result;
            return null;
        }


        public static void CilentTicked()
        {
            // foreach (var uuid in waitEntitys.Keys.ToArray())
            // {
            //     var entity = waitEntitys[uuid];
            //     var cmp = entity.Component as EntityComponent;
            //     //如果当前区块已经被加载且有tilemap存在
            //     if (_worldManage.world.HasTileMap(cmp.ChunkCoord))
            //     {
            //         if (!entitys.ContainsKey(uuid))
            //         {
            //             if (_worldManage is WorldHostService)
            //                 GD.Print($"【服务端】添加实体 {uuid}");
            //             if (_worldManage is WorldClientService)
            //                 GD.Print($"【客户端】添加实体 {uuid}");
            //
            //             entitys.Add(uuid, entity);
            //             if (entity.GetNode().GetParent() == null)
            //                 _worldManage.world.AddChild(entity.GetNode());
            //             entity.World = _worldManage.world;
            //             waitEntitys.Remove(uuid, out _);
            //         }
            //     }
            // }
            //
            // foreach (var uuid in entitys.Keys)
            // {
            //     var entity = entitys[uuid];
            //     var cmp = entity.Component as EntityComponent;
            //     if (!_worldManage.world.HasTileMap(cmp.ChunkCoord))
            //     {
            //         if (cmp.Belong == Player.Profile.Name)
            //             cmp.Belong = "";
            //     }
            // }
        }

        public static void UpdataEntitys(string uuid, EntityComponent data)
        {
            // if (_worldManage is WorldHostService)
            //     GD.Print($"【服务端】 <- 更新实体 {uuid} ");
            // if (_worldManage is WorldClientService)
            //     GD.Print($"【客户端】 -> 更新实体 {uuid}");
            //
            // GD.Print($"--> data {data.Belong} , {data.Id} , 类型{data.GetType()}");
            //
            // GD.Print();
            //
            //
            // try
            // {
            //     //卡在这里，两个GD.print都没输出
            //     if (entitys.ContainsKey(uuid))
            //     {
            //         GD.Print($"已有数据");
            //     }
            //     else
            //     {
            //         GD.Print($"Not Have Data");
            //     }
            //
            //     if (entitys.TryGetValue(uuid, out var entity))
            //     {
            //         GD.Print($"更新列表实体 {uuid}");
            //         entity.Component = data;
            //     }
            //     else
            //     {
            //         entity = CreateEntity(data, uuid);
            //         GD.Print($"添加进待加载列表 {uuid}");
            //         waitEntitys.TryAdd(uuid, entity);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine(e);
            //     throw;
            // }
        }

        public static IEntityNode GetEntity(string uuid)
        {
            //return entitys.GetValueOrDefault(uuid);\
            return null;
        }

        public static void ResetEntityOwned(string uuid)
        {
            // if (entitys.TryGetValue(uuid, out var entity))
            // {
            //     if (entity.Component is EntityComponent cmp)
            //         cmp.Belong = "";
            // }
            //
            // if (waitEntitys.TryGetValue(uuid, out entity))
            // {
            //     if (entity.Component is EntityComponent cmp)
            //         cmp.Belong = "";
            // }
        }

        public static void ResetEntitysUpdate()
        {
            // foreach (var uuid in entitys.Keys)
            //     if (entitys[uuid].Component is EntityComponent cmp)
            //         cmp.Update = false;
            // foreach (var uuid in waitEntitys.Keys)
            //     if (waitEntitys[uuid].Component is EntityComponent cmp)
            //         cmp.Update = false;
        }
    }
}