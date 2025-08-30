using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.Entity;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;


namespace horizoncraft.script.Features
{
    public class EntityManage
    {
        public static bool Enable = false;
        static WorldBase _worldManage;
        public static ConcurrentDictionary<string, Node2D> waitEntitys = new();
        public static System.Collections.Generic.Dictionary<string, Node2D> entitys = new();

        static EntityManage()
        {
        }

        public static void Init(WorldBase world)
        {
            if (Enable)
            {
                waitEntitys.Clear();
                entitys.Clear();
                GD.Print("切换场景，重新订阅");
            }
            else
            {
                Player.GetInformation.Add(() => $"已加载生物：{entitys.Count}");
                Player.GetInformation.Add(() => $"待加载生物：{waitEntitys.Count}");
                Player.GetInformation.Add(() => $"当前场景类型：{_worldManage.GetType().ToString()}");
            }

            Enable = true;
            _worldManage = world;
            _worldManage.OnChunkLoaded += OnWorldLoaded;
            _worldManage.OnChunkUnLoading += OnWorldUnLoading;
            _worldManage.world.CilentTicked += CilentTicked;
            //chunkManageSql.world.TileMapRemove += GetEntity;
        }

        public static EntityNode CreateEntity(Entitydata data, string uuid = null)
        {
            uuid ??= Guid.NewGuid().ToString();
            if (data.Uuid == "") data.Uuid = uuid;

            EntityMeta entityMeta = Materials.GetEntityMeta(data.Id);
            EntityNode entity = entityMeta.GetEntityNode();
            entity.Data = data;
            waitEntitys.TryAdd(data.Uuid, entity);
            return entity;
        }

        public static void OnWorldLoaded(WorldBase worldManage, Chunk chunk)
        {
            foreach (Entitydata data in chunk.entities)
            {
                var entity = CreateEntity(data);
                waitEntitys.TryAdd(data.Uuid, entity);
            }

            if (chunk.entities.Count > 0) GD.Print("[加载]实体：" + chunk.entities.Count);
            chunk.entities.Clear();
        }

        public static void OnWorldUnLoading(WorldBase worldManage, Chunk chunk) => GetEntity(chunk);

        public static void GetEntity(Chunk chunk)
        {
            GD.Print($"[EntityManage] {chunk.coord}");
            foreach (var uuid in entitys.Keys)
            {
                EntityNode entity = entitys[uuid] as EntityNode;
                if (entity != null && entity.Data.ChunkCoord == chunk.coord)
                {
                    chunk.entities.Add(entity.Data);
                    _worldManage.world.RemoveChild(entity);
                    if (entitys.Remove(uuid, out _))
                        entity.QueueFree();
                }
            }

            //删除待加载的实体
            foreach (var uuid in waitEntitys.Keys)
            {
                var entity = waitEntitys[uuid] as EntityNode;
                if (entity != null && entity.Data.ChunkCoord == chunk.coord)
                {
                    chunk.entities.Add(entity.Data);
                    entity.QueueFree();
                }
            }

            if (chunk.entities.Count > 0) GD.Print("保存实体：" + chunk.entities.Count);
        }

        public static List<Entitydata> GetMovedEntity(Vector2I coord)
        {
            List<Entitydata> result = new();
            foreach (var uuid in entitys.Keys)
            {
                EntityNode entity = entitys[uuid] as EntityNode;
                if (entity != null && entity.Data.ChunkCoord == coord)
                {
                    if (entity.Moveed)
                    {
                        result.Add(entity.Data);
                    }
                }
            }

            return result;
        }

        public static void CilentTicked()
        {
            foreach (var uuid in waitEntitys.Keys)
            {
                var entity = waitEntitys[uuid] as EntityNode;
                //如果当前区块已经被加载且有tilemap存在
                if (_worldManage.world.HasTileMap(entity.Data.ChunkCoord))
                {
                    if (!entitys.ContainsKey(uuid))
                    {
                        entitys.Add(uuid, entity);
                        if (entity.GetParent() == null)
                            _worldManage.world.AddChild(entity);
                        entity.world = _worldManage.world;
                        waitEntitys.Remove(uuid, out _);
                    }
                }
            }
        }

        public static void UpdataEntitys(string uuid, Entitydata data)
        {
            Node2D entity;
            entitys.TryGetValue(uuid, out entity);
            if (entity != null)
            {
                (entity as EntityNode).Data = data;
            }
            else
            {
                entity = CreateEntity(data, uuid);
                waitEntitys.TryAdd(uuid, entity);
            }
        }

        public static EntityNode GetEntity(string uuid)
        {
            Node2D entity;
            entitys.TryGetValue(uuid, out entity);
            if (entity != null) return (EntityNode)entity;
            return null;
        }
    }
}