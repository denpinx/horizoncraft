using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using Godot.Collections;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;
using horizoncraft.script.WorldControl.Tool;
using MemoryPack;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace horizoncraft.script;

//客户端RPC函数
public partial class World
{
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ConnectDone(string name, int peerid)
    {
        if (WorldService is { } worldbase)
            GD.Print($"[{worldbase.TickTimes}] ConnectDone({name},{peerid})");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OnMoveAChunk()
    {
        if (WorldService is IWorldService iws)
            iws.UpdateLoadChunkCoords();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetOpenBlockComponent(string name, byte[] bytes)
    {
        if (WorldService.PlayerService.Players.TryGetValue(name, out var playerData))
        {
            var scd = ByteTool.FromBytes<SetComponentData>(bytes);
            WorldService.SetOpenBlockComponent(playerData, scd);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetBlock(int x, int y, int z, int id, int state)
    {
        WorldService.SetBlock(new(x, y, z), Materials.Valueof(id), false, state);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdateChunk(int x, int y)
    {
        var pos = new Vector2I(x, y);
        if (WorldService.Chunks.ContainsKey(pos))
        {
            WorldService.Chunks[pos].update_server = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReceiveEntityPack(byte[] bytes)
    {
        EntityPack pack = ByteTool.FromBytes<EntityPack>(bytes);
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
        WorldService.EntityService.RemoveEntityData(uuid);
    }    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RemoveEntityDataOwned(string uuid)
    {
        WorldService.EntityService.RemoveEntityDataOwned(uuid);
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
}