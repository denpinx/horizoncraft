using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Features;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.WorldControl.Service;

public class WorldClientService : WorldBase, IWorldService, IWorldTickable, IWorldClientService
{
    private const int Port = 9999;


    public ConcurrentQueue<byte[]> ReciveChunkPacks = new();
    public Task ProcessReciveDataTask;

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
            GD.Print($"[客户端] 连接成功{id}");
            world.RpcId(1, "ConnectDone", Player.Profile.Name, world.Multiplayer.GetUniqueId());
            Connect = true;
        };
        world.Multiplayer.ConnectionFailed += () =>
        {
            GD.PrintErr($"[客户端] 连接失败！");
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
        LoadChunkQueue.Clear();
        Vector2I CenterCoord = world.player.playerData.ChunkCoord;
        for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
        {
            for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
            {
                Vector2I coord = new Vector2I(X, Y);
                LoadChunkQueue[coord] = new WorkBase();
            }
        }

        foreach (Vector2I coord in Chunks.Keys)
        {
            if (!LoadChunkQueue.ContainsKey(coord))
            {
                Chunk chunk = Chunks[coord];
                OffloadChunkQueue[coord] = chunk;
                Chunks.TryRemove(coord, out _);
            }
            else
            {
                LoadChunkQueue.TryRemove(coord, out _);
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

        if (name == Player.Profile.Name)
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
        if (!Connect || world.Multiplayer.MultiplayerPeer == null) return;
        if (playerData == null)
        {
            GD.PrintErr("playerData is null");
            return;
        }

        world.RpcId(1, "UpdataPlayer", playerData.Name, PlayerData.ToBytes(playerData));
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
        TickTimes++;
        if (world.player.playerData != null) SavePlayer(world.player.playerData);
        ProcessDataRecive();
        UpdateLoadChunkCoords();
        UpdataTileMap();
    }


    public override void SetBlock(Vector3I coord, BlockMeta meta, bool replaceAir = false, int state = 0)
    {
        if (!Connect) return;
        else
        {
            world.RpcId(1, "SetBlock", coord.X, coord.Y, coord.Z, meta.ID, state);
        }

        base.SetBlock(coord, meta, replaceAir, state);
    }

    public void ProcessDataRecive()
    {
        if (!Connect) return;
        if (!ReciveChunkPacks.IsEmpty)
            Interlocked.CompareExchange(ref ProcessReciveDataTask, Task.Run(() =>
            {
                byte[][] bytes = new byte[ReciveChunkPacks.Count][];
                ReciveChunkPacks.CopyTo(bytes, 0);
                ReciveChunkPacks.Clear();
                foreach (byte[] data in bytes)
                {
                    ChunkUpDataPackSet sync = ChunkUpDataPackSet.FromBytes(data);
                    for (int i = 0; i < sync.packs.Count; i++)
                    {
                        ChunkUpdataPack cup = sync.packs[i];
                        var coord = new Vector2I(cup.x, cup.y);
                        if (Chunks.ContainsKey(coord))
                        {
                            for (int j = 0; j < cup.updates.Count; j++)
                            {
                                Chunks[coord][cup.updates[j].x, cup.updates[j].y, cup.updates[j].z]
                                    .SetMeta(cup.updates[j].id);
                                Chunks[coord][cup.updates[j].x, cup.updates[j].y, cup.updates[j].z].STATE =
                                    cup.updates[j].state;
                            }
                        }
                    }
                }
            }), null);
    }
}