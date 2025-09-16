using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.Events.SystemEvents;
using horizoncraft.script.Expand;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.entity;

public class EntityServiceBase : IDisposable
{
    protected World World;

    //异步集合
    public ConcurrentDictionary<Guid, EntityData> EntityDatas = new();

    //主线程同步
    public Dictionary<Guid, IEntityNode> EntityNodes = new();

    public EntityServiceBase(World world)
    {
        World = world;
        world.timer.Timeout += Ticking;
        world.Service.ChunkService.OnChunkLoaded += OnChunkLoad;
        world.Service.ChunkService.OnChunkSaving += ExtractEntitiesFromChunk;
        PlayerNode.GetInformation[nameof(EntityServiceBase)] =
            () => $"加载实体:{EntityDatas.Count}" +
                  $"渲染实体:{EntityNodes.Count}";
    }

    public virtual void Ticking()
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
        }
    }

    public virtual void OnChunkLoad(Chunk chunk)
    {
        ReleaseChunkEntity(chunk);
    }

    public virtual void ReceiveEntityPack(EntityPack entityPack)
    {
        List<EntityDataSnapShot> call_back = new();
        foreach (var entity in entityPack.Entitys)
            UpdateEntityData(entity, call_back);
    }


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

    public List<EntityData> GetEntityByChunk(Vector2I coord)
    {
        var list = new List<EntityData>();
        foreach (var entity in EntityDatas.Values)
            if (entity.ChunkCoord == coord)
                list.Add(entity);

        return list;
    }

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

    public void ReleaseChunkEntity(Chunk chunk)
    {
        foreach (var entity in chunk.Entitys)
            EntityDatas.AddOrUpdate(entity.Uuid, entity, (key, value) => entity);
        chunk.Entitys.Clear();
    }

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

    public virtual void ProcessEntityNodeUpdate()
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
                entity.Owned = "";
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
                    entity.Update = true;
                    entity.Owned = PlayerNode.Profile.Name;
                }
            }
        }
    }

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
        if (data.Owned == "") data.Owned = PlayerNode.Profile.Name;
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

    public void Dispose()
    {
        World.timer.Timeout -= Ticking;
    }
}