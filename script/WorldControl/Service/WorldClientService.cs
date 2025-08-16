using System.Linq;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.WorldControl.Service;

public class WorldClientService : WorldBase, IWorldService, IWorldTickable
{
    private const int Port = 9999;

    public bool Init()
    {
        if (world == null) return false;
        EntityManage.Init(this);
        world.player.OnMoveToChunk += UpdateLoadChunkCoords;
        world.timer.Timeout += Tick;
        GD.Print($"[{TickTimes}] 连接服务器");
        //连接服务器
        world.Multiplayer.PeerConnected += id =>
        {
            GD.Print($"客户端连接成功{id}");
            world.RpcId(1, "ConnectDone", Player.LocalName, world.Multiplayer.GetUniqueId());
            Connect = true;
        };
        world.Multiplayer.ConnectionFailed += () =>
        {
            GD.PrintErr($"客户端连接失败！");
            Connect = false;
        };
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient("localhost", Port);
        world.Multiplayer.MultiplayerPeer = peer;
        return true;
    }

    public void ProcessChunkUnloadQueue()
    {
    }

    public void UpdateLoadChunkCoords()
    {
        if (!Connect) return;
        world.RpcId(1, "OnMoveAChunk");
        if (world.player.playerData == null) return;
        LoadingChunkQuee.Clear();
        Vector2I CenterCoord = world.player.playerData.ChunkCoord;
        for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
        {
            for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
            {
                Vector2I coord = new Vector2I(X, Y);
                LoadingChunkQuee[coord] = new WorkBase();
            }
        }

        foreach (Vector2I coord in LoadedChunks.Keys)
        {
            if (!LoadingChunkQuee.ContainsKey(coord))
            {
                Chunk chunk = LoadedChunks[coord];
                UnloadingQuee[coord] = chunk;
                LoadedChunks.TryRemove(coord, out _);
            }
            else
            {
                LoadingChunkQuee.TryRemove(coord, out _);
            }
        }
    }

    public void ProcessChunkLoadQueue()
    {
        if (!Connect) return;
    }

    public void ProcessPlayerLoadQueue()
    {
        if (!Connect) return;
    }

    public bool GetPlayer(string name, out PlayerData playerdata)
    {
        if (!Connect)
        {
            playerdata = null;
            return false;
        }

        if (name == Player.LocalName)
        {
            playerdata = world.player.playerData;
            return true;
        }

        if (Players.TryGetValue(name, out playerdata))
        {
            return true;
        }


        playerdata = null;
        return false;
    }

    public void SavePlayer(PlayerData playerData)
    {
        if (!Connect) return;
        if (playerData == null)
        {
            GD.PrintErr("playerData is null");
            return;
        }
        world.RpcId(1, "UpdataPlayer", playerData.Name, playerData.ToByte());
    }

    public void SaveChunk(Chunk chunk)
    {
        
    }

    public void Save()
    {
    }
    public void Tick()
    {
        if (!Connect) return;
        if(world.player.playerData!=null)SavePlayer(world.player.playerData);
        UpdateLoadChunkCoords();
        UpdataTileMap();
    }


    public override void SetBlock(Vector3I coord, BlockMeta meta, bool replaceAir = false, int state = 0)
    {
        if(!Connect)return;
        else
        {
            world.RpcId(1, "SetBlock", coord.X, coord.Y, coord.Z, meta.ID, state);
        }
        base.SetBlock(coord, meta, replaceAir, state);
    }
}