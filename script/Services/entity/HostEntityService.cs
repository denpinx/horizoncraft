using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Net;

namespace HorizonCraft.script.Services.entity;

public class HostEntityService : EntityServiceBase
{
    public HostEntityService(World world) : base(world)
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
            World.Service.EntityServiceNode.RpcId(player.PeerId, nameof(EntityServiceNode.ReceiveEntityPack),
                ByteTool.ToBytes<EntityPack>(pack));
        }
    }
}