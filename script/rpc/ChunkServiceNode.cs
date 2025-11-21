using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Net;
using Horizoncraft.script.Services.world;
using Horizoncraft.script.WorldControl;
using InventoryComponent = Horizoncraft.script.Components.BlockComponents.InventoryComponent;

namespace Horizoncraft.script.rpc;

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

    /// <summary>
    /// 接收单个全量更新区块，目前没用。
    /// </summary>
    /// <param name="data"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunk(byte[] data)
    {
        Chunk chunk = ByteTool.FromBytes<Chunk>(data);
        WorldService.ChunkService.Chunks[new(chunk.X, chunk.Y)] = chunk;
    }

    /// <summary>
    /// 接收全量更新区块区块集合
    /// </summary>
    /// <param name="data"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunkPack(byte[] data)
    {
        ChunkPack sync = ByteTool.FromBytes<ChunkPack>(data);
        for (int i = 0; i < sync.Chunks.Count; i++)
        {
            var chunk = sync.Chunks[i];
            chunk.update_tilemap = true;
            GD.Print($"#{chunk.coord} 区块全量更新");

            //同步区块
            WorldService.ChunkService.Chunks[chunk.coord] = chunk;
            //同步实体
            WorldService.EntityService.ReleaseChunkEntity(chunk);
        }
    }

    /// <summary>
    /// 接收区块的增量更新包
    /// </summary>
    /// <param name="data"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveChunkUpdatePack(byte[] data)
    {
        var pack = ByteTool.FromBytes<WorldSnapshot>(data);
        foreach (var update in pack.chunks)
        {
            if (World.Service.ChunkService.Chunks.TryGetValue(new Vector2I(update.X, update.Y), out Chunk chunk))
            {
                foreach (var vbd in update.VectorBlocks)
                    chunk.SetBlock(vbd.X, vbd.Y, vbd.Z, vbd.Block);

                GD.Print($"#{chunk.coord} 区块增量更新:{update.VectorBlocks.Count}个方块");
                foreach (var entity in update.Entiydatas)
                    WorldService.EntityService.AddEntityData(entity);
            }
        }
    }

    /// <summary>
    /// 接收世界时间
    /// </summary>
    /// <param name="time"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveWorldTime(int time)
    {
        WorldService.TickTimes = time;
    }

    /// <summary>
    /// 接收单个方块的更新,目前没啥用因为会自动增量更新。
    /// </summary>
    /// <param name="data"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveBlockData(byte[] data, int x, int y, int z)
    {
        BlockData block = ByteTool.FromBytes<BlockData>(data);
        WorldService.ChunkService.SetBlock(new Vector3I(x, y, z), block);
        if (World.PlayerNode.OpeningInventoryNode != null &&
            World.PlayerNode.playerData.OpenInventory == new System.Numerics.Vector3(x, y, z))
        {
            World.PlayerNode.OpeningInventoryNode.TargetBlock = block;
        }
        else if (World.PlayerNode.OpeningInventoryNode != null)
        {
        }
    }

    /// <summary>
    /// 接收当前打开的方块的完整状态，
    /// </summary>
    /// <param name="data"></param>
    /// <param name="playerinv"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReciveLookingBlockData(byte[] data, byte[] playerinv)
    {
        // TODO 待优化
        BlockData blockData = ByteTool.FromBytes<BlockData>(data);
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(playerinv);
        World.PlayerNode.playerData.Inventory = inv;

        if (World.PlayerNode.OpeningInventoryNode != null)
        {
            World.PlayerNode.OpeningInventoryNode.TargetBlock = blockData;
        }
        else
        {
            GD.Print("收到数据,打开菜单");

            if (World.PlayerNode.OpeningInventoryNode != null)
            {
                World.PlayerNode.RemoveChild(World.PlayerNode.OpeningInventoryNode);
            }

            var block = blockData;
            if (block == null) return;
            World.PlayerNode.OpeningInventoryNode =
                InventoryManage.GetInventory<InventoryNode>(block.GetComponent<InventoryComponent>().InventoryName);
            World.PlayerNode.OpeningInventoryNode.TargetBlock = block;
            World.PlayerNode.OpeningInventoryNode.PlayerNode = World.PlayerNode;
            World.PlayerNode.AddChild(World.PlayerNode.OpeningInventoryNode);
        }
    }

    /// <summary>
    /// 客户端调用，重新获取区块。
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReGetChunk(int x, int y)
    {
        var pos = new Vector2I(x, y);
        if (WorldService.ChunkService.Chunks.ContainsKey(pos))
        {
            WorldService.ChunkService.Chunks[pos].update_server = true;
        }
    }

    /// <summary>
    /// 客户端调用，设置当前打开方块的组件
    /// </summary>
    /// <param name="name">玩家名</param>
    /// <param name="bytes"></param>
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

    /// <summary>
    /// 客户端调用，设置方块
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="id"></param>
    /// <param name="state"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetBlock(int x, int y, int z, string id, int state)
    {
        WorldService.ChunkService.SetBlock(new(x, y, z), Materials.Valueof(id), state);
    }
}