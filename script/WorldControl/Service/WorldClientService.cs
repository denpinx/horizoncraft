using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Features;
using horizoncraft.script.Inventory;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Tool;

namespace HorizonCraft.script.WorldControl.Service;

public class WorldClientService : WorldBase, IWorldService, IWorldTickable, IWorldClientService
{
    private const int Port = 9999;
    public PlayerData LastFarmeData = null;

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

        var player = PlayerdataSnapshot.ToSnapshot(playerData);

        if (LastFarmeData == null || LastFarmeData.Position != playerData.Position)
            world.RpcId(1, "UpdataPlayer", playerData.Name, ByteTool.ToBytes<PlayerdataSnapshot>(player));
        if (LastFarmeData == null) LastFarmeData = new PlayerData();
        LastFarmeData.Position = playerData.Position;

        //world.RpcId(1, "UpdataPlayer", playerData.Name, ByteTool.ToBytes<PlayerdataSnapshot>(player));
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
        
        UpdateLights();
        UpdataTileMap();
    }

    public override bool PickItem(PlayerData playerdata, InventoryBase inventory, int index)
    {
        world.player.playerData.OpeningBlockInventory = true;
        if (world.player.ShowView == null) return false;
        if (inventory is BlockInventory bi)
            world.RpcId(1, "PickBlockInvItem",
                playerdata.Name, index);
        else
            world.RpcId(1, "PickInvItem", playerdata.Name, index);

        // if (inventory is BlockInventory bi)
        //     world.RpcId(1, "InvokePlayerFunc",
        //         playerdata.Name, "pick_block_inv_item", index);
        // else
        //     world.RpcId(1, "InvokePlayerFunc", playerdata.Name, "pick_inv_item", index);


        return true;
    }

    public void OpenBlockView(Blockdata blockdata)
    {
        if (world == null || world.player.playerData == null) return;
        if (world.player.ShowView != null)
            world.player.RemoveChild(world.player.ShowView);

        world.player.ShowView =
            InventoryManage.GetInventory<InventoryNode>(blockdata.GetComponent<InventoryComponent>().InventoryName);
        world.player.ShowView.TargetBlock = blockdata;
        world.player.ShowView.player = world.player;
        world.player.AddChild(world.player.ShowView);
    }

    public override void CloseView()
    {
        world.player.playerData.OpeningBlockInventory = false;
        world.RpcId(1, "CloseBlockInv", Player.Profile.Name);
        //world.RpcId(1, "InvokePlayerFunc", Player.Profile.Name, "close_inventory");
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
                foreach (byte[] data in ReciveChunkPacks)
                {
                    WorldSnapshot sync = ByteTool.FromBytes<WorldSnapshot>(data);
                    for (int i = 0; i < sync.chunks.Count; i++)
                    {
                        ChunkSnapshot cup = sync.chunks[i];
                        var coord = new Vector2I(cup.x, cup.y);
                        if (Chunks.ContainsKey(coord))
                        {
                            if (Chunks[coord].version <= cup.version)
                            {
                                Chunks[coord].version = cup.version;
                                for (int j = 0; j < cup.list.Count; j++)
                                {
                                    var item = cup.list[j];
                                    var block = Chunks[coord].GetBlock(item.x, item.y, item.z);
                                    block.SetMeta(item.id);
                                    block.STATE = item.state;
                                }
                            }
                            else
                            {
                                GD.Print($"异常同步！新版本{cup.version} : 旧版本{Chunks[coord].version}");
                            }
                        }
                    }
                }

                ReciveChunkPacks.Clear();
            }), null);
    }

    public override void SetOpenBlockComponent(PlayerData playerData, SetComponentData data)
    {
        world.RpcId(1, "SetOpenBlockComponent", playerData.Name, ByteTool.ToBytes(data));
    }

    public override void CraftGridRecipeItem(PlayerData player)
    {
        world.RpcId(1, "CraftGridRecipeItem", player.Name);
    }
}