using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Entity;
using Horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using Horizoncraft.script.Services.message;

namespace HorizonCraft.script.Services.world;

public class ClientWorldService : WorldServiceBase
{
    public static bool Connected = false;
    public static int Port = 9999;
    public static string ip = "localhost";

    public ClientWorldService(World world) : base(world)
    {
        world.Multiplayer.PeerConnected += id =>
        {
            Connected = true;
            GD.Print($"[客户端] 连接成功 #{id}");
        };
        world.Multiplayer.ConnectionFailed += () =>
        {
            Connected = false;
            ConnectFailed();
            GD.PrintErr($"[客户端] 连接失败.");
        };
        world.Multiplayer.ServerDisconnected += () =>
        {
            Connected = false;
            ConnectFailed();
            GD.Print("[客户端] 服务器断开.");
        };


        var enet = new ENetMultiplayerPeer();
        enet.CreateClient(ip, Port);
        world.Multiplayer.MultiplayerPeer = enet;
    }

    public override void InitializeServices()
    {
        EntityBehavior = new ClientEntityBehavior();
        ;
        ChunkService = AddService<ClientChunkService>(new ClientChunkService(World));
        PlayerService = AddService<ClientPlayerService>(new ClientPlayerService(World));
        EntityService = AddService<ClientEntityService>(new ClientEntityService(World));
        MessageService = AddService<ClientMessageService>(new ClientMessageService(World));
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(ClientWorldService)}");
    }

    private void ConnectFailed()
    {
        World?.GetTree().ChangeSceneToFile("res://tscn/Menu/MainMenu.tscn");
    }

    public override void LoadWorldProfile()
    {
    }
}