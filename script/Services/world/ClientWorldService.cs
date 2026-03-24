using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components;
using Horizoncraft.script.Entity;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.Recipes;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.Services.entity;
using Horizoncraft.script.Services.message;
using Horizoncraft.script.Services.player;
using Horizoncraft.script.WorldControl;
using Microsoft.Extensions.DependencyInjection;

namespace Horizoncraft.script.Services.world;

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
        var result = enet.CreateClient(ip, Port);
        if (result == Error.Ok)
        {
            world.Multiplayer.MultiplayerPeer = enet;
        }
        else
        {
            GD.PrintErr($"客户端连接失败 {result}");
        }
        ServiceCollection.AddTransient<NeoComponentManager, NeoComponentManager>();
        ServiceCollection.AddTransient<NeoWorldGenerator, NeoWorldGenerator>();
        ServiceCollection.AddTransient<NeoLootTable, NeoLootTable>();
        ServiceCollection.AddTransient<NeoRecipeManage, NeoRecipeManage>();
        ServiceCollection.AddTransient<ChunkServiceBase, ClientChunkService>();
        ServiceCollection.AddTransient<PlayerServiceBase, ClientPlayerService>();
        ServiceCollection.AddTransient<EntityServiceBase, ClientEntityService>();
        ServiceCollection.AddTransient<MessageServiceBase, ClientMessageService>();
        ServiceCollection.AddTransient<EntityBehaviorBase, ClientEntityBehavior>();
        ServiceProvider = ServiceCollection.BuildServiceProvider();
    }

    private void ConnectFailed()
    {
        World?.GetTree().ChangeSceneToFile("res://tscn/Menu/MainMenu.tscn");
    }

    public override void LoadWorldProfile()
    {
    }
}