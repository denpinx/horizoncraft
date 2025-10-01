using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using HorizonCraft.script.Services.world;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.rpc;

/// <summary>
/// 区块类相关RPC操作
/// </summary>
public partial class ChunkServiceNode : Node
{
    private World World;

    private WorldServiceBase WorldService => this.World.Service;

    public ChunkServiceNode(World world)
    {
        this.Name = nameof(ChunkServiceNode);
        World = world;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunk(byte[] data)
    {
        Chunk chunk = ByteTool.FromBytes<Chunk>(data);
        WorldService.ChunkService.Chunks[new(chunk.X, chunk.Y)] = chunk;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunkPack(byte[] data)
    {
        ChunkPack sync = ByteTool.FromBytes<ChunkPack>(data);
        for (int i = 0; i < sync.Chunks.Count; i++)
        {
            WorldService.ChunkService.Chunks[sync.Chunks[i].coord] = sync.Chunks[i];
            WorldService.EntityService.ReleaseChunkEntity(sync.Chunks[i]);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunkUpdatePack(byte[] data)
    {
        var pack = ByteTool.FromBytes<WorldSnapshot>(data);
        foreach (var update in pack.chunks)
        {
            if (World.Service.ChunkService.Chunks.TryGetValue(new Vector2I(update.X, update.Y), out Chunk chunk2))
            {
                
                foreach (var block in update.list)
                {
                    chunk2.SetBlock(block.x, block.y, block.z, Materials.BlockMetas[block.id], block.state);
                }
                GD.Print("chunk2+recive:"+chunk2.coord.ToString(),"update.list"+update.Entiydatas.Count);
                foreach (var entity in update.Entiydatas)
                    WorldService.EntityService.AddEntityData(entity);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveWorldTime(int time)
    {
        WorldService.TickTimes = time;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveBlockData(byte[] data, int x, int y, int z)
    {
        BlockData block = ByteTool.FromBytes<BlockData>(data);
        WorldService.ChunkService.SetBlock(new Vector3I(x, y, z), block);
        if (World.PlayerNode.ShowView != null &&
            World.PlayerNode.playerData.OpenInventory == new System.Numerics.Vector3(x, y, z))
        {
            World.PlayerNode.ShowView.TargetBlock = block;
        }
        else if (World.PlayerNode.ShowView != null)
        {
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveLookingBlockData(byte[] data, byte[] playerinv)
    {
        // TODO 待优化
        BlockData blockData = ByteTool.FromBytes<BlockData>(data);
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(playerinv);
        World.PlayerNode.playerData.Inventory = inv;

        if (World.PlayerNode.ShowView != null)
        {
            World.PlayerNode.ShowView.TargetBlock = blockData;
        }
        else
        {
            GD.Print("收到数据,打开菜单");
            
            if (World.PlayerNode.ShowView != null)
            {
                World.PlayerNode.RemoveChild(World.PlayerNode.ShowView);
            }
            var block = blockData;
            if (block == null) return;
            World.PlayerNode.ShowView = InventoryManage.GetInventory<InventoryNode>(block.GetComponent<InventoryComponent>().InventoryName);
            World.PlayerNode.ShowView.TargetBlock = block;
            World.PlayerNode.ShowView.PlayerNode = World.PlayerNode;
            World.PlayerNode.AddChild(World.PlayerNode.ShowView);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReGetChunk(int x, int y)
    {
        var pos = new Vector2I(x, y);
        if (WorldService.ChunkService.Chunks.ContainsKey(pos))
        {
            WorldService.ChunkService.Chunks[pos].update_server = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetOpenBlockComponent(string name, byte[] bytes)
    {
        if (WorldService.PlayerService.Players.TryGetValue(name, out var playerData))
        {
            var scd = ByteTool.FromBytes<SetComponentData>(bytes);
            var sobc = new PlayerSetBlockComponentEvent()
            {
                world = World,
                Player = playerData,
                ComponentData = scd,
            };
            WorldService.PlayerService.Events.SetOpenBlockComponent(sobc);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetBlock(int x, int y, int z, int id, int state)
    {
        WorldService.ChunkService.SetBlock(new(x, y, z), Materials.Valueof(id), state);
    }
}