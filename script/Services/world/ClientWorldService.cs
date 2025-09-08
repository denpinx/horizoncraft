using Godot;
using horizoncraft.script;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;

namespace HorizonCraft.script.Services.world;

public class ClientWorldService : WorldServiceBase
{
    public static int Port = 9999;
    public static string ip = "localhost";

    public ClientWorldService(World world) : base(world)
    {
        world.Multiplayer.PeerConnected += id =>
        {
            GD.Print($"[客户端] 连接成功{id}");
            world.RpcId(1, "ConnectDone", PlayerNode.Profile.Name, world.Multiplayer.GetUniqueId());
        };
        world.Multiplayer.ConnectionFailed += () =>
        {
            GD.PrintErr($"[客户端] 连接失败！");
        };
        world.Multiplayer.ServerDisconnected += () =>
        {
            World.worldMode = World.WorldMode.Single;
            world.GetTree().ChangeSceneToFile("res://tscn/Menu/MainMenu.tscn");
            GD.Print("【客户端】服务器断开");
        };
        
        
        var enet = new ENetMultiplayerPeer();
        enet.CreateClient(ip, Port);
        world.Multiplayer.MultiplayerPeer = enet;
    }

    public override void InitializeServices()
    {
        ChunkService = new ClientChunkService(World);
        PlayerService = new ClientPlayerService(World);
        EntityService = new ClientEntityService(World);
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(ClientWorldService)}");
    }
}