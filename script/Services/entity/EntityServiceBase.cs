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
            ComponentManager.ExecuteComponents(ese);
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
            if(entity.Name!=name)continue;
            
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

    public void ProcessEntityNodeUpdate()
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
                    continue;
                }

                EntityNodes.Add(uuid, node);
                World.AddChild(node.GetNode());
            }

            var Entity = EntityDatas[uuid];
            if (!World.HasTileMap(Entity.ChunkCoord))
            {
                if (EntityNodes.ContainsKey(uuid))
                {
                    var node = EntityNodes[uuid];
                    EntityNodes.Remove(uuid);
                    node.GetNode().QueueFree();
                }
            }
        }
    }

    private IEntityNode SpawnEntity(EntityData data)
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

    public void UpdateEntityData(EntityDataSnapShot data, List<EntityDataSnapShot> CallBack = null)
    {
        //默认是主机玩家
        if (data.Owned == "") data.Owned = PlayerNode.Profile.Name;
        if (EntityDatas.TryGetValue(data.Uuid, out var entity))
        {
            if (entity.Owned != data.Owned)
            {
                if (World.Service.PlayerService.Players.TryGetValue(data.Owned, out var player))
                {
                    World.Service.EntityServiceNode.RpcId(player.PeerId,
                        nameof(EntityServiceNode.RemoveEntityDataOwned),
                        entity.Uuid.ToByteArray());
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
        else if (World.Service.PlayerService.Players.TryGetValue(data.Owned, out var player))
        {
            World.Service.EntityServiceNode.RpcId(player.PeerId,
                nameof(EntityServiceNode.RemoveEntityData),
                entity.Uuid.ToString());
        }
    }

    public void AddEntityData(EntityData data)
    {
        //默认是主机玩家
        if (data.Owned == "") data.Owned = PlayerNode.Profile.Name;
        if (data.Uuid == Guid.Empty) data.Uuid = System.Guid.NewGuid();
        data.Update = false;
        EntityDatas.AddOrUpdate(data.Uuid, data, (key, old) => data);
    }

    public void RemoveEntityData(Guid uuid)
    {
        if (EntityDatas.TryRemove(uuid, out var data))
        {
            data.Removed = true;
        }
    }

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