using Godot;
using System;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.player;
using HorizonCraft.script.Services.world;

/// <summary>
/// 将World中的Rpc函数全部单独拿出来作为节点调用
/// </summary>
public partial class PlayerServiceNode : Node
{
    private World World;

    private WorldServiceBase WorldService => World.Service;
    private PlayerServiceBase PlayerService => World.Service.PlayerService;
    private ChunkServiceBase ChunkService => World.Service.ChunkService;

    public PlayerServiceNode(World world)
    {
        this.Name = nameof(PlayerServiceNode);
        World = world;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdataPlayer(string name, byte[] bytes)
    {
        PlayerDataSnapshot playerData = ByteTool.FromBytes<PlayerDataSnapshot>(bytes);
        if (name == PlayerNode.Profile.Name)
        {
            World.PlayerNode.Position = new Vector2(playerData.X, playerData.Y);
        }
        else
        {
            PlayerService.UpdatePlayer(playerData);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void GetPlayer(string name, int peerid)
    {
        GD.Print($"[请求获取玩家信息]{name},{peerid}");
        PlayerData playerData;
        if (World.Service is HostWorldService)
        {
            if (PlayerService.GetPlayerOrLoad(name, out playerData))
            {
                playerData.PeerId = peerid;
                var bytes = ByteTool.ToBytes(playerData);
                GD.Print("已传回玩家数据");
                RpcId(peerid, nameof(ReceivePlayer), bytes);
            }
            else
            {
                GD.Print("无玩家数据");
            }
        }
    }

    /// <summary>
    /// 接收来自服务端发来的全量玩家数据
    /// </summary>
    /// <param name="data"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReceivePlayer(byte[] data)
    {
        GD.Print("ReceivePlayer 接收到来自服务端的玩家数据");
        PlayerData playerData = ByteTool.FromBytes<PlayerData>(data);
        if (playerData.Name == PlayerNode.Profile.Name)
        {
            if (World.PlayerNode.playerData == null)
            {
                World.PlayerNode.playerData = playerData;
                if (World.Service is ClientWorldService)
                {
                    World.PlayerNode.Position = playerData.Position_v2;
                }
            }
        }

        PlayerService.Players.AddOrUpdate(playerData.Name, playerData, (key, value) => playerData);
    }

    /// <summary>
    /// 同步来自服务端的玩家增量更新包
    /// </summary>
    /// <param name="data"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReceivePlayerDatas(byte[] data)
    {
        PlayerPack playerPack = ByteTool.FromBytes<PlayerPack>(data);
        for (int i = 0; i < playerPack.players.Count; i++)
        {
            var player = playerPack.players[i];
            PlayerService.UpdatePlayer(player);
        }
    }

    /// <summary>
    /// 接收服务端传来的物品栏数据
    /// </summary>
    /// <param name="data"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReceivePlayerInv(byte[] data)
    {
        if (World.PlayerNode.playerData == null) return;
        PlayerInventory inv = ByteTool.FromBytes<PlayerInventory>(data);
        World.PlayerNode.playerData.Inventory = inv;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void BreakBlockEvent(string player, int x, int y, int z)
    {
        if (PlayerService.Players.TryGetValue(player, out var playerdata))
        {
            var eve = new PlayerBreakblockEvent()
            {
                world = WorldService.World,
                Player = playerdata,
                Position = new(x, y, z)
            };
            PlayerService.Events.BreakBlock(eve);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PlaceBlockEvent(string player, int x, int y, int z)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerdata))
        {
            var eve = new PlayerPlaceBlockEvent()
            {
                world = WorldService.World,
                Player = playerdata,
                Position = new(x, y, z)
            };
            WorldService.PlayerService.Events.PlaceBlock(eve);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PlayerUseItemEvent(string player, int x, int y, int z)
    {
        if (WorldService.PlayerService.Players.TryGetValue(player, out var playerdata))
        {
            var puie = new PlayerUseItemEvent()
            {
                world = WorldService.World,
                Player = playerdata,
                UseItemStack = playerdata.Inventory.GetHandItemStack(),
                Position = new(x, y, z)
            };
            WorldService.PlayerService.Events.UseItem(puie);
        }
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void DropItemEvent(string player, bool all)
    {
        if (all) WorldService.PlayerService.Events.DropAllItem(WorldService, player);
        else WorldService.PlayerService.Events.DropItem(WorldService, player);
    }
}