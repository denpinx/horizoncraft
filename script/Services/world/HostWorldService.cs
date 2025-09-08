using Godot;
using horizoncraft.script;
using horizoncraft.script.Net;
using horizoncraft.script.NewProxy.player;
using HorizonCraft.script.Services.chunk;
using HorizonCraft.script.Services.entity;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.Services.world;

public class HostWorldService : WorldServiceBase
{
    private WorldProfile Profile;
    public static int Port = 9999;
    public static int MaxPlayer = 10;

    public HostWorldService(World world) : base(world)
    {

        
        
        world.Multiplayer.PeerDisconnected += OnPlayerExit;
        var enet = new ENetMultiplayerPeer();
        enet.CreateServer(Port, MaxPlayer);
        world.Multiplayer.MultiplayerPeer = enet;
        using (var conn = SqliteTool.InitSqlite())
        {
            if (conn.CheckWorldProfileExists("WorldProfile"))
            {
                Profile = conn.GetWorldProfileByteData("WorldProfile");
                TickTimes = (int)Profile.Time;
            }
            else
            {
                Profile = new WorldProfile();
            }
        }
    }

    public override void InitializeServices()
    {
        ChunkService = new HostChunkService(World);
        PlayerService = new HostPlayerService(World);
        EntityService = new HostEntityService(World);
        InitializeNode();

        GD.Print($"[初始化完成]{nameof(HostWorldService)}");
    }

    private void OnPlayerExit(long id)
    {
        foreach (var player in PlayerService.Players.Values)
        {
            if (player.PeerId == id)
            {
                PlayerService.Players.TryRemove(player.Name, out var _);
                return;
            }
        }
    }
}