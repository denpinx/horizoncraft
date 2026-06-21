using Godot;
using System;
using Horizoncraft.script;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Net;
using Horizoncraft.script.Utility;
using Horizoncraft.script.Services.world;

/// <summary>
/// 实体类相关RPC操作
/// </summary>
public partial class EntityServiceNode : Node
{
    private World World;
    private WorldServiceBase WorldService;

    public EntityServiceNode(World world)
    {
        this.Name = nameof(EntityServiceNode);
        World = world;
        WorldService = world.Service;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ClientReceiveEntityPack(byte[] bytes)
    {
        EntityPack pack = ByteTool.FromBytes<EntityPack>(bytes);
        GameLogger.Info("EntityService","ClientReceiveEntityPack"+pack.Entitys.Count);
        WorldService.EntityService.ReceiveEntityPack(pack);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ServerReceiveEntityPack(byte[] bytes)
    {
        EntityPack pack = ByteTool.FromBytes<EntityPack>(bytes);
        GameLogger.Info("EntityService","ServerReceiveEntityPack"+pack.Entitys.Count);
        WorldService.EntityService.ReceiveEntityPack(pack);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ResetEntityOwned(byte[] bytes)
    {
        var pack = ByteTool.FromBytes<UUIDPack>(bytes);
        WorldService.EntityService.ResetEntityOwned(pack);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RemoveEntityData(string uuid)
    {
        WorldService.EntityService.RemoveEntityData(Guid.Parse(uuid));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RemoveEntityDataOwned(string uuid)
    {
        WorldService.EntityService.RemoveEntityDataOwned(Guid.Parse(uuid));
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void AddEntityData(byte[] bytes)
    {
        WorldService.EntityService.AddEntityData(ByteTool.FromBytes<EntityData>(bytes));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdateEntityData(byte[] bytes)
    {
        WorldService.EntityService.UpdateEntityData(ByteTool.FromBytes<EntityDataSnapShot>(bytes));
    }

    /// <summary>
    /// 接收所有应该同步的实体集合
    /// </summary>
    /// <param name="bytes">EntityUuidPack的序列化结果</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveAllNeedEntityUuid(byte[] bytes)
    {
        EntityUuidPack euuidPack = ByteTool.FromBytes<EntityUuidPack>(bytes);
        foreach (var uuid in WorldService.EntityService.EntityDatas.Keys)
        {
            if (!euuidPack.Uuids.Contains(uuid))
            {
                WorldService.EntityService.RemoveEntityData(uuid);
                GameLogger.Info("EntityService",$"remove uuid {uuid}");
            }
        }
    }
}