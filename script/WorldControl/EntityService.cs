using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using horizoncraft.script.Entity;
using horizoncraft.script.Net;
using HorizonCraft.script.WorldControl.Service;

namespace horizoncraft.script.WorldControl;

/// <summary>
/// 已实装
/// </summary>
public class EntityService
{
    WorldBase WorldService;
    bool IsClient = false;

    //异步集合
    public ConcurrentDictionary<string, EntityData> EntityDatas = new();

    //主线程同步
    public Dictionary<string, IEntityNode> EntityNodes = new();

    public EntityService(WorldBase worldService)
    {
        this.WorldService = worldService;
        var lambda = () => $"所有实体数据：{EntityDatas.Count}\n实列化实体：{EntityNodes.Count}";
        Player.GetInformation["EntityService"] = lambda;

        worldService.OnChunkLoaded += OnChunkLoad;
        if (worldService is WorldClientService)
        {
            IsClient = true;
            worldService.OnChunkUnLoading += OnClientChunkUnloading;
            worldService.OnTicked += ProcessOnClient;
        }
        else
        {
            worldService.OnChunkUnLoading += OnHostChunkUnloading;
            worldService.OnTicked += ProcessOnHost;
        }
    }

    /// <summary>
    /// 获取实体数据
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public EntityData GetEntityData(string uuid)
    {
        EntityDatas.TryGetValue(uuid, out var value);
        if (value != null) return value;
        PrintErr($"GetEntityData {uuid} 不存在");
        return null;
    }

    /// <summary>
    /// 获取实体的渲染节点
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public IEntityNode GetEntityNode(string uuid)
    {
        EntityNodes.TryGetValue(uuid, out var value);
        if (value != null) return value;
        PrintErr($"GetEntityNode {uuid} 不存在");
        return null;
    }

    /// <summary>
    /// 获取区块范围内的所有实体uuid
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <returns>区块内所有实体uuid</returns>
    public List<string> GetUuidByChunk(Vector2I coord)
    {
        var list = new List<string>();
        foreach (var uuid in EntityDatas.Keys)
        {
            var entity = EntityDatas[uuid];
            if (entity.ChunkCoord == coord)
            {
                list.Add(entity.Uuid);
            }
        }

        return list;
    }


    /// <summary>
    /// 获取指定区块内的所有实体
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <returns>区块内的实体集合</returns>
    public List<EntityData> GetEntityByChunk(Vector2I coord)
    {
        var list = new List<EntityData>();
        foreach (var uuid in EntityDatas.Keys)
        {
            var entity = EntityDatas[uuid];
            if (entity.ChunkCoord == coord)
            {
                list.Add(entity);
            }
        }

        return list;
    }

    /// <summary>
    /// 抓取区块范围内的所有实体到区块内
    /// 会把已经加载的实体移动至区块内部
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <returns></returns>
    public void ExtractEntitiesFromChunk(Chunk chunk)
    {
        chunk.Entitys.Clear();
        foreach (var uuid in EntityDatas.Keys.ToArray())
        {
            var entity = EntityDatas[uuid];
            if (entity.ChunkCoord == chunk.coord)
            {
                chunk.Entitys.Add(entity);
                EntityDatas.TryRemove(uuid, out _);
            }
        }
    }

    /// <summary>
    /// 释放区块内的实体数据到管理器内
    /// </summary>
    /// <param name="chunk"></param>
    public void ReleaseChunkEntity(Chunk chunk)
    {
        foreach (var entity in chunk.Entitys)
            EntityDatas.AddOrUpdate(entity.Uuid, entity, (key, value) => entity);
        chunk.Entitys.Clear();
    }


    /// <summary>
    /// 区块加载时
    /// </summary>
    /// <param name="chunk"></param>
    public void OnChunkLoad(Chunk chunk)
    {
        ReleaseChunkEntity(chunk);
        if (chunk.Entitys.Count > 0) PrintLog($"加载区块({chunk.coord})的实体{chunk.Entitys.Count}个");
    }

    /// <summary>
    /// 客户端的区块卸载事件
    /// </summary>
    /// <param name="chunk"></param>
    public void OnClientChunkUnloading(Chunk chunk)
    {
        var uuids = GetUuidByChunk(chunk.coord);
        if (uuids.Count == 0) return;
        UUIDPack pack = new()
        {
            uuids = uuids
        };
        WorldService.world.RpcId(1, "ResetEntityOwned", ByteTool.ToBytes(pack));
    }

    /// <summary>
    /// 服务端的以及单机模式区块卸载事件
    /// </summary>
    /// <param name="chunk"></param>
    public void OnHostChunkUnloading(Chunk chunk)
    {
        ExtractEntitiesFromChunk(chunk);
    }

    /// <summary>
    /// 获取区块内移动过的实体,如果是无所属者的实体也会被拾取
    /// 注意，这一步是在准备所有区块的更变阶段调用的
    /// </summary>
    /// <param name="coord">区块坐标</param>
    public List<EntityData> GetChunkMovedEntity(Vector2I coord)
    {
        var list = new List<EntityData>();
        foreach (var Entity in EntityDatas.Values)
        {
            if (Entity.Update)
            {
                list.Add(Entity);
                Entity.Update = false;
            }
            else if (Entity.Owned == "")
            {
                list.Add(Entity);
            }
        }

        return list;
    }

    /// <summary>
    /// 客户端主线程处理
    /// </summary>
    private void ProcessOnClient()
    {
        EntityPack entityPack = new EntityPack();
        foreach (var uuid in EntityDatas.Keys)
        {
            //更新数据
            if (EntityNodes.ContainsKey(uuid))
            {
                EntityNodes[uuid].Entity = EntityDatas[uuid];
            }
            else
            {
                // TODO 实列化新的节点实列
                var node = SpawnEntity(EntityDatas[uuid]);
                if (node == null)
                {
                    PrintErr($"ProcessOnClient 创建失败(uuid:{uuid},name:{EntityDatas[uuid].Name})");
                    return;
                }

                EntityNodes.Add(uuid, node);
                WorldService.world.AddChild(node.GetNode());
            }

            var Entity = EntityDatas[uuid];
            if (!WorldService.world.HasTileMap(Entity.ChunkCoord))
            {
                if (EntityNodes.ContainsKey(uuid))
                {
                    var node = EntityNodes[uuid];
                    EntityNodes.Remove(uuid);
                    node.GetNode().QueueFree();
                }
            }

            //上传由自己代理的实体
            if (Entity.Owned == Player.Profile.Name&&Entity.Update)
            {
                entityPack.Entitys.Add(Entity);
                Entity.Update = false;
            }
        }

        if (entityPack.Entitys.Count > 0)
        {
            WorldService.world.RpcId(1, "ReceiveEntityPack", ByteTool.ToBytes<EntityPack>(entityPack));
        }

        //删除不存在的节点
        foreach (var uuid in EntityNodes.Keys.ToArray())
        {
            if (!EntityDatas.ContainsKey(uuid))
            {
                var entity = EntityNodes[uuid];
                EntityNodes.Remove(uuid);
                entity.GetNode().QueueFree();
            }
        }
    }

    /// <summary>
    /// 服务端主线程处理
    /// </summary>
    /// 0.05秒执行一次
    private void ProcessOnHost()
    {
        foreach (var uuid in EntityDatas.Keys)
        {
            //更新数据
            if (EntityNodes.ContainsKey(uuid))
            {
                EntityNodes[uuid].Entity = EntityDatas[uuid];
            }
            else
            {
                var node = SpawnEntity(EntityDatas[uuid]);
                if (node == null)
                {
                    PrintErr($"ProcessOnHost 创建失败(uuid:{uuid},name:{EntityDatas[uuid].Name})");
                    return;
                }

                EntityNodes.Add(uuid, node);
                WorldService.world.AddChild(node.GetNode());
            }

            //剥夺所属权
            var Entity = EntityDatas[uuid];
            if (!WorldService.world.HasTileMap(Entity.ChunkCoord))
            {
                if (Entity.Owned == Player.Profile.Name)
                {
                    Entity.Owned = "";
                    Entity.Update = true;
                }
                else if (Entity.Owned != "")
                {
                    if (!WorldService.Players.ContainsKey(Entity.Owned))
                    {
                        PrintLog($"ProcessOnHost 剥夺所属权(uuid:{uuid},owned:{Entity.Owned})");
                        Entity.Owned = "";
                        Entity.Update = true;
                    }
                }

                if (EntityNodes.ContainsKey(uuid))
                {
                    var node = EntityNodes[uuid];
                    EntityNodes.Remove(uuid);
                    node.GetNode().QueueFree();
                }
            }
        }

        //删除不存在的节点
        foreach (var uuid in EntityNodes.Keys.ToArray())
        {
            if (!EntityDatas.ContainsKey(uuid))
            {
                var entity = EntityNodes[uuid];
                EntityNodes.Remove(uuid);
                entity.GetNode().QueueFree();
            }
        }
    }

    /// <summary>
    /// 根据Entitydata生成实体
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private IEntityNode SpawnEntity(EntityData data)
    {
        if (Materials.DictionaryEntityMetas.TryGetValue(data.Name, out var meta))
        {
            var node = meta.GetEntityNode();
            node.Entity = data;
            if (data.Uuid == "") data.Uuid = System.Guid.NewGuid().ToString();
            return node;
        }

        return null;
    }

    /// <summary>
    /// 接收实体包
    /// </summary>
    /// <param name="entityPack"></param>
    public void ReceiveEntityPack(EntityPack entityPack)
    {
        foreach (var entity in entityPack.Entitys)
        {
            EntityDatas.AddOrUpdate(entity.Uuid, entity, (key, old) => entity);
        }
    }

    /// <summary>
    /// 重置给予的uuid实体
    /// </summary>
    /// <param name="uuidPack"></param>
    public void ResetEntityOwned(UUIDPack uuidPack)
    {
        foreach (var uuid in uuidPack.uuids)
        {
            if (EntityDatas.TryGetValue(uuid, out var Entity))
            {
                Entity.Owned = "";
            }
        }
    }

    /// <summary>
    /// 添加实体数据
    /// </summary>
    /// <param name="data"></param>
    public void AddEntityData(EntityData data)
    {
        //默认是主机玩家
        if (data.Owned == "") data.Owned = Player.Profile.Name;
        if (data.Uuid == "") data.Uuid = System.Guid.NewGuid().ToString();
        EntityDatas.AddOrUpdate(data.Uuid, data, (key, old) => data);
    }

    /// <summary>
    /// 删除指定的uuid的实体
    /// </summary>
    /// <param name="uuid"></param>
    public void RemoveEntityData(string uuid)
    {
        if (IsClient)
            WorldService.world.RpcId(1, "RemoveEntityData", uuid);
        EntityDatas.TryRemove(uuid, out _);
    }

    private void PrintLog(string msg)
    {
        GD.Print($"{WorldService.TickTimes} - [EntityService] {msg}");
    }

    private void PrintErr(string msg)
    {
        GD.PrintErr($"{WorldService.TickTimes} - [EntityService] {msg}");
    }
}