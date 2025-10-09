using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.Events.SystemEvents;
using horizoncraft.script.Expand;
using horizoncraft.script.Net;
using horizoncraft.script.Services;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.entity;

/// <summary>
/// 基础实体服务
/// 提供完整的实体管理，实体视图同步，实体组件处理。实体生命周期管理服务。
/// </summary>
public class EntityServiceBase : ServiceBase
{
    /// <summary>
    /// 已加载的实体模型集合
    /// </summary>
    public ConcurrentDictionary<Guid, EntityData> EntityDatas = new();

    /// <summary>
    /// 已加载的实体视图集合
    /// </summary>
    public Dictionary<Guid, IEntityNode> EntityNodes = new();

    public EntityServiceBase(World world) : base(world)
    {
        world.timer.Timeout += Ticking;
        world.Service.ChunkService.OnChunkLoaded += OnChunkLoad;
        world.Service.ChunkService.OnChunkSaving += ExtractEntitiesFromChunk;
        PlayerNode.GetInformation[nameof(EntityServiceBase)] = () =>
        {
            StringBuilder sb = new();
            sb.Append($"加载实体数:{EntityDatas.Count}\n");
            sb.Append($"实体节点数:{EntityNodes.Count}\n");

            foreach (var data in EntityDatas.Values)
            {
                sb.Append($"{data.Name} : {data.Owned}\n");
            }

            return sb.ToString();
        };
    }

    /// <summary>
    /// 时刻处理
    /// </summary>
    protected virtual void Ticking()
    {
        ProcessEntityNode();
        ProcessEntityNodeUpdate();

        foreach (var entity in EntityDatas.Values)
        {
            if (entity.Removed) continue;
            EntitySystemEvent ese = new()
            {
                Service = World.Service,
                EntityData = entity,
            };
            ComponentManager.ExecuteEntityComponents(ese);
            //尝试加载该区块
            if (!World.Service.ChunkService.Chunks.ContainsKey(entity.ChunkCoord))
            {
                World.Service.ChunkService.LoadChunkQueue.Enqueue(entity.ChunkCoord);
            }
        }
    }

    protected virtual void OnChunkLoad(Chunk chunk)
    {
        ReleaseChunkEntity(chunk);
    }

    /// <summary>
    /// 接收可能来自服务端或则客户端的实体快照更新包
    /// </summary>
    /// <param name="entityPack"></param>
    public virtual void ReceiveEntityPack(EntityPack entityPack)
    {
        List<EntityDataSnapShot> call_back = new();
        foreach (var entity in entityPack.Entitys)
            UpdateEntityData(entity, call_back);
    }

    /// <summary>
    /// 获取指定区块范围内的所有实体的guid
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <returns></returns>
    public List<Guid> GetUuidByChunk(Vector2I coord)
    {
        var list = new List<Guid>();
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
    /// 获取指定坐标范围内的指定名称的实体集合
    /// </summary>
    /// <param name="coord">全局像素坐标</param>
    /// <param name="range">范围</param>
    /// <param name="name">实体名称</param>
    /// <returns>实体集合,不会为null，但是可能数量为0</returns>
    public List<EntityData> GetEntityInRangeByName(Vector2I coord, int range, string name)
    {
        var list = new List<EntityData>();
        foreach (var entity in EntityDatas.Values)
        {
            if (entity.Name != name) continue;

            var pos = (entity.Position.ToVector2I() - coord).Abs();
            if (pos.X < range && pos.Y < range)
            {
                list.Add(entity);
            }
        }

        return list;
    }

    /// <summary>
    /// 获取指定坐标半径内的实体集合
    /// </summary>
    /// <param name="coord">像素坐标</param>
    /// <param name="range">半径</param>
    /// <returns>实体集合,不会为null，但是可能数量为0</returns>
    public List<EntityData> GetEntityInRange(Vector2I coord, int range)
    {
        var list = new List<EntityData>();
        foreach (var entity in EntityDatas.Values)
        {
            var pos = (entity.Position.ToVector2I() - coord).Abs();
            if (pos.X < range && pos.Y < range)
            {
                list.Add(entity);
            }
        }

        return list;
    }

    /// <summary>
    /// 获取指定区块坐标内的所有实体
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <returns>集合可能为空，不会为null</returns>
    public List<EntityData> GetEntityByChunk(Vector2I coord)
    {
        var list = new List<EntityData>();
        foreach (var entity in EntityDatas.Values)
            if (entity.ChunkCoord == coord)
                list.Add(entity);

        return list;
    }

    /// <summary>
    /// 提取实体到区块中
    /// </summary>
    /// <param name="chunk">区块</param>
    public void ExtractEntitiesFromChunk(Chunk chunk)
    {
        chunk.Entitys.Clear();
        foreach (var uuid in EntityDatas.Keys.ToArray())
        {
            var entity = EntityDatas[uuid];
            if (entity.ChunkCoord == chunk.coord)
            {
                entity.Owned = "";
                chunk.Entitys.Add(entity);
                EntityDatas.TryRemove(uuid, out _);
            }
        }
    }

    /// <summary>
    /// 释放区块实体到实体服务中
    /// </summary>
    /// <param name="chunk"></param>
    public void ReleaseChunkEntity(Chunk chunk)
    {
        foreach (var entity in chunk.Entitys)
            EntityDatas.AddOrUpdate(entity.Uuid, entity, (key, value) => entity);
        chunk.Entitys.Clear();
    }

    /// <summary>
    /// 获取指定区块内移动或更新过的实体
    /// </summary>
    /// <param name="coord"></param>
    /// <returns>集合可能为空，但不会为null</returns>
    public List<EntityData> GetChunkMovedEntity(Vector2I coord)
    {
        var list = new List<EntityData>();
        foreach (var entity in EntityDatas.Values)
        {
            if (entity.ChunkCoord == coord)
            {
                if (entity.Update)
                {
                    list.Add(entity);
                    entity.Update = false;
                }
                else if (entity.Owned == "")
                {
                    list.Add(entity);
                }
            }
        }

        return list;
    }

    /// <summary>
    /// 处理实体节点
    /// </summary>
    public void ProcessEntityNode()
    {
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
    /// 处理实体节点更新
    /// </summary>
    public virtual void ProcessEntityNodeUpdate()
    {
        foreach (var uuid in EntityDatas.Keys.ToArray())
        {
            //更新数据
            if (EntityNodes.ContainsKey(uuid))
            {
                EntityNodes[uuid].Entity = EntityDatas[uuid];
            }
            else
            {
                //创建视图
                var node = SpawnEntity(EntityDatas[uuid]);
                if (node == null)
                {
                    continue;
                }

                EntityNodes.Add(uuid, node);
                World.AddChild(node.GetNode());
            }

            //卸载不可见实体视图,更新所属权
            var entity = EntityDatas[uuid];
            if (!World.HasTileMap(entity.ChunkCoord))
            {
                if (entity.Owned == PlayerNode.Profile.Name)
                {
                    entity.Owned = "";
                    entity.Update = true;
                }

                if (EntityNodes.ContainsKey(uuid))
                {
                    var node = EntityNodes[uuid];
                    EntityNodes.Remove(uuid);
                    node.GetNode().QueueFree();
                }
            }
            else
            {
                //服务端拿回所属权
                if (entity.Owned != PlayerNode.Profile.Name)
                {
                    //更新状态，同步回客户端
                    entity.Owned = PlayerNode.Profile.Name;
                    entity.Update = true;
                }
            }
        }
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected IEntityNode SpawnEntity(EntityData data)
    {
        if (Materials.DictionaryEntityMetas.TryGetValue(data.Name, out var meta))
        {
            var node = meta.GetEntityNode();
            node.Entity = data;
            if (data.Uuid == Guid.Empty) data.Uuid = System.Guid.NewGuid();
            return node;
        }

        return null;
    }

    /// <summary>
    /// 重置给予的uuid的实体的所属权
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
    /// 更新实体数据
    /// </summary>
    /// <param name="data">实体快照</param>
    /// <param name="CallBack">更新失败回调</param>
    public virtual void UpdateEntityData(EntityDataSnapShot data, List<EntityDataSnapShot> CallBack = null)
    {
        //默认是主机玩家
        if (data.Owned == "") data.Owned = PlayerNode.Profile.Name;
        if (EntityDatas.TryGetValue(data.Uuid, out var entity))
        {
            if (entity.Owned != data.Owned)
            {
                //删除客户端的uuid
                if (World.Service.PlayerService.Players.TryGetValue(data.Owned, out var player))
                {
                    World.Service.EntityServiceNode.RpcId(player.PeerId,
                        nameof(EntityServiceNode.RemoveEntityDataOwned),
                        entity.Uuid.ToString());
                }
            }
            else
            {
                var block = World.Service.ChunkService.GetBlock(new Vector3I(data.Coord.X, data.Coord.Y, 1));
                if (block != null && !block.BlockMeta.Collide) //防止穿墙
                {
                    if (entity.Position != data.Position)
                    {
                        entity.Position = data.Position;
                        entity.Update = true;
                    }
                }
                else
                {
                    data.Position = entity.Position;
                    CallBack?.Add(data);
                }
            }
        }
        //删除
        else if (World.Service.PlayerService.Players.TryGetValue(data.Owned, out var player))
        {
            World.Service.EntityServiceNode.RpcId(player.PeerId,
                nameof(EntityServiceNode.RemoveEntityData),
                data.Uuid.ToString());
        }
    }

    /// <summary>
    /// 添加实体数据
    /// </summary>
    /// <param name="data"></param>
    public virtual void AddEntityData(EntityData data)
    {
        //默认是主机玩家
        if (data.Owned == "")
        {
            data.Owned = PlayerNode.Profile.Name;
        }

        if (data.Uuid == Guid.Empty) data.Uuid = System.Guid.NewGuid();
        //data.Update = false;
        EntityDatas.AddOrUpdate(data.Uuid, data, (key, old) => data);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="uuid"></param>
    public void RemoveEntityData(Guid uuid)
    {
        if (EntityDatas.TryRemove(uuid, out var data))
        {
            data.Removed = true;
        }
    }

    /// <summary>
    /// 删除实体的所属权
    /// </summary>
    /// <param name="uuid"></param>
    public void RemoveEntityDataOwned(Guid uuid)
    {
        if (EntityDatas.TryGetValue(uuid, out var entity))
        {
            entity.Owned = "";
        }
    }
}