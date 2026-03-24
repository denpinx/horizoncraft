using System.Collections.Generic;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Net;
using Horizoncraft.script.NewProxy.player;

namespace Horizoncraft.script.Services.entity;

public class ClientEntityService : EntityServiceBase
{
    public ClientEntityService(World world, NeoComponentManager componentManager) : base(world, componentManager)
    {
        
    }

    protected override void Ticking()
    {
        ProcessEntityNode();
        ProcessEntityNodeUpdate();
    }

    public override void ProcessEntityNodeUpdate()
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
                //entity.Owned = "";
                if (EntityNodes.ContainsKey(uuid))
                {
                    var node = EntityNodes[uuid];
                    EntityNodes.Remove(uuid);
                    node.GetNode().QueueFree();
                }

                EntityDatas.TryRemove(uuid, out _);
            }
        }
    }

    public override void UpdateEntityData(EntityDataSnapShot data, List<EntityDataSnapShot> CallBack = null)
    {
        GD.Print("UpdateEntityData" + data.Uuid);
        //默认是主机玩家
        if (EntityDatas.TryGetValue(data.Uuid, out var entity))
        {
            if (entity.Position != data.Position)
            {
                entity.Position = data.Position;
                //entity.Update = true;
            }
        }
    }

    public override void AddEntityData(EntityData data)
    {
        data.Update = false;
        EntityDatas.AddOrUpdate(data.Uuid, data, (key, old) => data);
    }

    public override void ReceiveEntityPack(EntityPack entityPack)
    {
        foreach (var entity in entityPack.Entitys)
            UpdateEntityData(entity);
    }
}