using System.Collections.Generic;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Net;

namespace Horizoncraft.script.Services.entity;

public class HostEntityService : EntityServiceBase
{
    public HostEntityService(World world, NeoComponentManager componentManager) : base(world, componentManager)
    {
        
    }

    public override void ReceiveEntityPack(EntityPack entityPack)
    {
        List<EntityDataSnapShot> call_back = new();
        foreach (var entity in entityPack.Entitys)
            UpdateEntityData(entity, call_back);
        
        
        //异常回溯
        if (call_back.Count > 0 &&
            World.Service.PlayerService.Players.TryGetValue(entityPack.From, out var player))
        {
            var pack = new EntityPack()
            {
                From = PlayerNode.Profile.Name,
                Entitys = call_back
            };
            World.Service.EntityServiceNode.RpcId(player.PeerId, nameof(EntityServiceNode.ClientReceiveEntityPack),
                ByteTool.ToBytes<EntityPack>(pack));
        }
    }
}