using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Net;
using Horizoncraft.script.rpc;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.chunk;

public class HostChunkService : ChunkServiceBase
{
    public WorldSnapshot WorldSnapshot = new WorldSnapshot();

    private readonly Dictionary<int, ChunkPack> _wholeChunkUpdate = new();
    private readonly Dictionary<int, WorldSnapshot> _diffUpdate = new();

    public HostChunkService(World world,NeoWorldGenerator worldGenerator) : base(world,worldGenerator)
    {
        PlayerNode.GetInformation[nameof(HostChunkService)] =
            () => $"差异更新区块:{WorldSnapshot.chunks.Count}";
    }

    public override void Ticking()
    {
        base.Ticking();
        CalculateDifference();
        SyncChunks();
    }

    /// <summary>
    /// 同步区块
    /// </summary>
    public void SyncChunks()
    {
        _wholeChunkUpdate.Clear();
        _diffUpdate.Clear();

        //差异更新
        foreach (var chunk in WorldSnapshot.chunks)
        {
            if (chunk.VectorBlocks.Count == 0 && chunk.Entiydatas.Count == 0) continue;

            foreach (var playerset in World.Service.PlayerService.Players)
            {
                PlayerData pd1 = playerset.Value;
                //按距离同步
                if (
                    Math.Abs(chunk.X - pd1.ChunkCoord.X) <= LoadHorizon &&
                    Math.Abs(chunk.Y - pd1.ChunkCoord.Y) <= LoadHorizon
                )
                {
                    if (!_diffUpdate.TryGetValue(pd1.PeerId, out var snap))
                    {
                        snap = new WorldSnapshot();
                        _diffUpdate[pd1.PeerId] = snap;
                    }
                    chunk.ResetEmptyOwned(pd1.Name); //更新实体的从属
                    snap.chunks.Add(chunk);
                }
            }
        }

        foreach (var player in World.Service.PlayerService.Players.Values) player.EntityUuidPack.Uuids.Clear();
        //全量更新
        foreach (var chunk in Chunks.Values)
        {
            //这里更新的是，玩家客户端应该加载的所有实体名单
            foreach (var player in World.Service.PlayerService.Players.Values)
            {
                var result = World.Service.EntityService.GetEntityByChunk(chunk.coord);
                foreach (var entity in result)
                {
                    player.EntityUuidPack.Uuids.Add(entity.Uuid);
                }
            }

            if (chunk.ServerFullUpdate)
            {
                //下一帧同步这个区块内的玩家
                World.Service.PlayerService.ResetPlayerMoveStateByChunk(chunk.coord);
                foreach (var playerset in World.Service.PlayerService.Players)
                {
                    PlayerData pd1 = playerset.Value;
                    //按距离同步
                    if (
                        Math.Abs(chunk.X - pd1.ChunkCoord.X) <= LoadHorizon &&
                        Math.Abs(chunk.Y - pd1.ChunkCoord.Y) <= LoadHorizon
                    )
                    {
                        if (pd1.Name != PlayerNode.Profile.Name)
                        {
                            if (!_wholeChunkUpdate.TryGetValue(pd1.PeerId, out var pack))
                            {
                                pack = new ChunkPack();
                                _wholeChunkUpdate[pd1.PeerId] = pack;
                            }

                            pack.Chunks.Add(chunk);
                        }
                    }
                }
            }
        }

        foreach (var chunk in Chunks.Values)
            chunk.ServerFullUpdate = false;
        foreach (var kv in _wholeChunkUpdate)
        {
            var bytes = ByteTool.ToBytes<ChunkPack>(kv.Value);
            World.Service.ChunkServiceNode.RpcId(kv.Key,
                nameof(ChunkServiceNode.ReciveChunkPack),
                bytes);
        }

        foreach (var kv in _diffUpdate)
        {
            if (kv.Key != 0)
            {
                if (World.Multiplayer.GetPeers().Contains(kv.Key))
                {
                    var bytes = ByteTool.ToBytes<WorldSnapshot>(kv.Value);
                    World.Service.ChunkServiceNode.RpcId(kv.Key,
                        nameof(ChunkServiceNode.ReciveChunkUpdatePack),
                        bytes);
                }
            }
        }

        //确保服务端的实体被删除后，客户端也能够知道
        foreach (var player in World.Service.PlayerService.Players.Values)
        {
            if (player.PeerId != 0)
            {
                if (!player.LastFarmeEntityUuidPack.Equals(player.EntityUuidPack))
                {
                    World.Service.EntityServiceNode.RpcId(player.PeerId,
                        nameof(EntityServiceNode.ReciveAllNeedEntityUuid),
                        ByteTool.ToBytes(player.EntityUuidPack));
                }

                player.LastFarmeEntityUuidPack = player.EntityUuidPack;
                player.EntityUuidPack = new EntityUuidPack();
            }
        }

        WorldSnapshot.chunks.Clear();
    }

    public void CalculateDifference()
    {
        foreach (var chunk in Chunks.Values)
        {
            if (chunk.UpdateList.Count > 0)
            {
                var cs = new ChunkSnapshot()
                {
                    Version = World.Service.TickTimes,
                    X = chunk.coord.X,
                    Y = chunk.coord.Y,
                };

                foreach (var v in chunk.UpdateList)
                {
                    cs.VectorBlocks.Add(new VectorBlockData()
                    {
                        X = (byte)v.X,
                        Y = (byte)v.Y,
                        Z = (byte)v.Z,
                        Block = chunk.GetBlock(v.X, v.Y, v.Z),
                    });
                }

                WorldSnapshot.chunks.Add(cs);
            }
            else
            {
                var result = World.Service.EntityService.GetChunkMovedEntity(chunk.coord);
                if (result.Count > 0)
                {
                    var cs = new ChunkSnapshot()
                    {
                        Version = World.Service.TickTimes,
                        X = chunk.coord.X,
                        Y = chunk.coord.Y,
                        Entiydatas = result,
                    };
                    WorldSnapshot.chunks.Add(cs);
                }
            }
        }
    }
}