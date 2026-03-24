using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Events.SystemEvents;
using Horizoncraft.script.Expand;
using Horizoncraft.script.Net;
using Horizoncraft.script.Services;
using Horizoncraft.script.Services.world;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.entity;

/// <summary>
/// 基础实体服务
/// 提供完整的实体管理，实体视图同步，实体组件处理。实体生命周期管理服务。
/// </summary>
public class EntityServiceBase
{
    public WorldServiceBase WorldService;
    
    /// <summary>
    /// 已加载的实体模型集合
    /// </summary>
    public ConcurrentDictionary<Guid, EntityData> EntityDatas = new();

    /// <summary>
    /// 已加载的实体视图集合
    /// </summary>
    public Dictionary<Guid, IEntityNode> EntityNodes = new();

    protected NeoComponentManager componentManager;
    protected World World;
    public EntityServiceBase(World world,NeoComponentManager componentManager)
    {
        this.World = world;
        this.componentManager = componentManager;
        WorldService = world.Service;
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
    /// 每帧主逻辑更新入口。
    /// <list type="bullet">
    ///   <item>清理无效实体节点</item>
    ///   <item>同步实体数据到视图节点</item>
    ///   <item>执行所有实体的组件系统（<see cref="ComponentManager.ExecuteEntityComponents"/>)</item>
    ///   <item>确保实体所在区块已加载（否则加入加载队列）</item>
    /// </list>
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
            componentManager.ExecuteEntityComponents(ese);
            // ComponentManager.ExecuteEntityComponents(ese);
            //尝试加载该区块
            if (!World.Service.ChunkService.Chunks.ContainsKey(entity.ChunkCoord))
            {
                World.Service.ChunkService.LoadChunkQueue.Enqueue(entity.ChunkCoord);
            }
        }
    }
    /// <summary>
    /// 响应区块加载完成事件，将区块中暂存的实体释放到实体服务中。
    /// </summary>
    /// <param name="chunk">已加载的区块</param>
    protected virtual void OnChunkLoad(Chunk chunk)
    {
        ReleaseChunkEntity(chunk);
    }

    /// <summary>
    /// 处理来自网络的实体快照更新包（通常由服务端广播或客户端同步）。
    /// <para>逐个应用快照到本地实体数据，并处理所有权变更或位置校验。</para>
    /// </summary>
    /// <param name="entityPack">包含多个实体快照的网络包</param>
    public virtual void ReceiveEntityPack(EntityPack entityPack)
    {
        List<EntityDataSnapShot> call_back = new();
        foreach (var entity in entityPack.Entitys)
            UpdateEntityData(entity, call_back);
    }

    /// <summary>
    /// 获取指定区块坐标内所有实体的 UUID 列表。
    /// </summary>
    /// <param name="coord">区块坐标（以 Chunk.Size 为单位）</param>
    /// <returns>UUID 列表，可能为空但永不为 <c>null</c></returns>
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
    /// 获取指定像素坐标范围内、具有特定名称的实体集合。
    /// </summary>
    /// <param name="coord">中心像素坐标（X, Y）</param>
    /// <param name="range">正方形范围半径（单位：像素）</param>
    /// <param name="name">目标实体名称（区分大小写）</param>
    /// <returns>匹配的实体列表，可能为空但永不为 <c>null</c></returns>
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
    /// 获取指定像素坐标半径内的所有实体。
    /// </summary>
    /// <param name="coord">中心像素坐标（X, Y）</param>
    /// <param name="range">正方形范围半径（单位：像素）</param>
    /// <returns>实体列表，可能为空但永不为 <c>null</c></returns>
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
    /// 在区块保存前，将属于该区块的所有实体从内存移出并暂存至区块对象中。
    /// <para>用于持久化或跨会话传输。</para>
    /// </summary>
    /// <param name="chunk">即将保存的区块</param>
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
    /// 将区块中暂存的实体重新注册到实体服务中（通常在区块加载后调用）。
    /// </summary>
    /// <param name="chunk">已加载的区块，其 <see cref="Chunk.Entitys"/> 包含待恢复的实体</param>
    public void ReleaseChunkEntity(Chunk chunk)
    {
        foreach (var entity in chunk.Entitys)
            EntityDatas.AddOrUpdate(entity.Uuid, entity, (key, value) => entity);
        chunk.Entitys.Clear();
    }

    /// <summary>
    /// 获取指定区块内需要同步的“变动实体”——包括：
    /// <list type="bullet">
    ///   <item>设置了 <see cref="EntityData.Update"/> 标志的实体</item>
    ///   <item>所有权为空（<see cref="EntityData.Owned"/> == ""）的实体（表示需重新分配）</item>
    /// </list>
    /// <para>常用于网络同步或存档前的状态收集。</para>
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <returns>需同步的实体列表，可能为空但永不为 <c>null</c></returns>
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
    /// 清理已删除实体对应的渲染节点。
    /// <para>确保 <see cref="EntityNodes"/> 与 <see cref="EntityDatas"/> 保持一致。</para>
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
    /// 同步实体数据到渲染节点，并管理实体可见性与所有权。
    /// <list type="bullet">
    ///   <item>若实体所在区块未加载，则卸载其视图节点</item>
    ///   <item>若区块已加载且本地玩家拥有该实体，则标记为可更新</item>
    ///   <item>自动创建缺失的视图节点</item>
    /// </list>
    /// </summary>
    public virtual void ProcessEntityNodeUpdate()
    {
        foreach (var kvp in EntityDatas)
        {
            var uuid = kvp.Key;
            var entity_data = kvp.Value;
            //更新数据
            if (EntityNodes.ContainsKey(uuid))
            {
                EntityNodes[uuid].Entity = entity_data;
            }
            else
            {
                //创建视图
                var node = SpawnEntity(entity_data);
                if (node == null)
                {
                    continue;
                }

                EntityNodes.Add(uuid, node);
                World.AddChild(node.GetNode());
            }

            //卸载不可见实体视图,更新所属权
            var entity = entity_data;
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
    /// 根据实体元数据（<see cref="Materials.DictionaryEntityMetas"/>）创建对应的渲染节点。
    /// </summary>
    /// <param name="data">实体数据，包含名称与初始状态</param>
    /// <returns>初始化后的实体节点，若元数据未注册则返回 <c>null</c></returns>
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
    /// 批量重置指定 UUID 实体的所有权为空（""），表示放弃控制权。
    /// <para>通常由服务端调用，用于回收客户端离线实体的控制权。</para>
    /// </summary>
    /// <param name="uuidPack">包含待重置 UUID 的网络包</param>
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
    /// 应用实体快照更新，处理位置校验、所有权转移与冲突回调。
    /// <para>若目标位置存在碰撞方块，则拒绝移动并加入回调列表供重试。</para>
    /// </summary>
    /// <param name="data">来自网络的实体快照</param>
    /// <param name="CallBack">因碰撞等原因未能应用的快照列表（可选）</param>
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
    /// 添加新实体到服务中。
    /// <para>自动分配 UUID 和默认所有权（当前玩家）。</para>
    /// </summary>
    /// <param name="data">待添加的实体数据</param>
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
    /// 标记指定 UUID 的实体为“已删除”。
    /// <para>实体数据仍保留在字典中一帧，以便组件系统完成清理，下一帧由 <see cref="ProcessEntityNode"/> 移除视图。</para>
    /// </summary>
    /// <param name="uuid">要删除的实体 UUID</param>
    public void RemoveEntityData(Guid uuid)
    {
        if (EntityDatas.TryRemove(uuid, out var data))
        {
            data.Removed = true;
        }
    }

    /// <summary>
    /// 清除指定实体的所有权（设为 ""），使其变为“无主”状态。
    /// <para>常用于玩家断开连接或实体移交控制权。</para>
    /// </summary>
    /// <param name="uuid">目标实体 UUID</param>
    public void RemoveEntityDataOwned(Guid uuid)
    {
        if (EntityDatas.TryGetValue(uuid, out var entity))
        {
            entity.Owned = "";
        }
    }
}