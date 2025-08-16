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
    public class EntityManage : IBaseManage
    {
        public static bool Enable = false; 
        static WorldBase _worldManage;
        public static ConcurrentBag<Node2D> waitEntitys = new();
        public static List<Node2D> entitys = new();

        static EntityManage()
        {
        }

        public static void Init(WorldBase world)
        {
            Enable = true;
            _worldManage = world;
            _worldManage.OnChunkLoaded += OnWorldLoaded;
            _worldManage.OnChunkUnLoading += OnWorldUnLoading;
            _worldManage.world.CilentTicked += CilentTicked;
            //chunkManageSql.world.TileMapRemove += GetEntity;
            //UI调试信息注册
            Player.GetInformation.Add(() => $"已加载生物：{entitys.Count}");
            Player.GetInformation.Add(() => $"待加载生物：{waitEntitys.Count}");
        }

        public static void OnWorldLoaded(WorldBase worldManage, Chunk chunk)
        {
            foreach (Entitydata data in chunk.entities)
            {
                EntityMeta entityMeta = Materials.GetEntityMeta(data.id);
                EntityNode entity = entityMeta.GetEntityNode();
                entity.Data = data;
                waitEntitys.Add(entity);
            }

            if (chunk.entities.Count > 0) GD.Print("[加载]实体：" + chunk.entities.Count);
            chunk.entities.Clear();
        }

        public static void OnWorldUnLoading(WorldBase worldManage, Chunk chunk) => GetEntity(chunk);

        public static void GetEntity(Chunk chunk)
        {
            List<Node2D> rml = new();
            //chunk.entities.Clear();
            for (int i = entitys.Count - 1; i >= 0; i--)
            {
                EntityNode entity = entitys[i] as EntityNode;
                if (entity != null && entity.Data.ChunkCoord == chunk.coord)
                {
                    chunk.entities.Add(entity.Data);
                    _worldManage.world.RemoveChild(entity);
                    rml.Add(entity);
                }
            }

            for (int i = 0; i < rml.Count; i++)
            {
                entitys.Remove(rml[i]);
                rml[i].QueueFree();
            }

            for (int i = waitEntitys.Count - 1; i >= 0; i--)
            {
                Node2D node2D;
                if (waitEntitys.TryTake(out node2D))
                {
                    var entity = node2D as EntityNode;
                    if (entity != null && entity.Data.ChunkCoord == chunk.coord)
                    {
                        chunk.entities.Add(entity.Data);
                        entity.QueueFree();
                    }
                    else
                    {
                        waitEntitys.Add(node2D);
                    }
                }
            }

            if (chunk.entities.Count > 0) GD.Print("保存实体：" + chunk.entities.Count);
        }

        public static void CilentTicked()
        {
            List<Node2D> es = new List<Node2D>();
            for (int i = 0; i < waitEntitys.Count; i++)
            {
                Node2D entity;
                if (waitEntitys.TryTake(out entity))
                {
                    EntityNode entityNode = entity as EntityNode;
                    if (_worldManage.world.HasTileMap(entityNode.Data.ChunkCoord))
                    {
                        entitys.Add(entity);
                        if (entity.GetParent() == null)
                            _worldManage.world.AddChild(entity);
                        entityNode.world = _worldManage.world;
                    }
                    else
                    {
                        es.Add(entity);
                    }
                }
            }

            foreach (Node2D n2d in es) waitEntitys.Add(n2d);
        }
    }
}