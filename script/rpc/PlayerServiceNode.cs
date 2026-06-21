using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Events.player;
using Horizoncraft.script.Inventory;
using Horizoncraft.script.Net;
using Horizoncraft.script.Utility;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.player;
using Horizoncraft.script.Services.world;

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
        PlayerData playerData;
        GameLogger.Info("PlayerService",$"[{nameof(PlayerServiceNode)}] 服务端收到玩家数据获取请求 @{name,16} #{peerid,8}");
        if (World.Service is HostWorldService)
        {
            if (PlayerService.GetPlayerOrLoad(name, out playerData))
            {
                playerData.PeerId = peerid;
                var bytes = ByteTool.ToBytes(playerData);
                RpcId(peerid, nameof(ReceivePlayer), bytes);
                GameLogger.Info("PlayerService",$"[{nameof(PlayerServiceNode)}] 服务端已传回玩家数据");
            }
            else
            {
                GameLogger.Info("PlayerService",$"[{nameof(PlayerServiceNode)}] 服务端暂无该玩家数据");
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
        PlayerData playerData = ByteTool.FromBytes<PlayerData>(data);
        GameLogger.Info("PlayerService",$"[{nameof(PlayerServiceNode)}] 客户端收到玩家数据 @{playerData.Name}");
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