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
        //全量更新
        Dictionary<int, ChunkPack> wholeChunkUpdate = new();
        //差异更新
        Dictionary<int, WorldSnapshot> diffUpdate = new();

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
                    if (!diffUpdate.ContainsKey(pd1.PeerId))
                        diffUpdate[pd1.PeerId] = new WorldSnapshot();
                    chunk.ResetEmptyOwned(pd1.Name); //更新实体的从属
                    diffUpdate[pd1.PeerId].chunks.Add(chunk);
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

            if (chunk.update_server)
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
                            if (!wholeChunkUpdate.ContainsKey(pd1.PeerId))
                            {
                                var result = World.Service.EntityService.GetEntityByChunk(chunk.coord);
                                chunk.Entitys = result;
                                wholeChunkUpdate[pd1.PeerId] = new ChunkPack();
                            }

                            wholeChunkUpdate[pd1.PeerId].Chunks.Add(chunk);
                        }
                    }
                }
            }
        }

        foreach (var chunk in Chunks.Values)
            chunk.update_server = false;
        foreach (var key in wholeChunkUpdate.Keys)
        {
            var bytes = ByteTool.ToBytes<ChunkPack>(wholeChunkUpdate[key]);
            World.Service.ChunkServiceNode.RpcId(key,
                nameof(ChunkServiceNode.ReciveChunkPack),
                bytes);
        }

        foreach (var key in diffUpdate.Keys)
        {
            if (key != 0)
            {
                if (World.Multiplayer.GetPeers().Contains(key))
                {
                    var bytes = ByteTool.ToBytes<WorldSnapshot>(diffUpdate[key]);
                    World.Service.ChunkServiceNode.RpcId(key,
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
            ChunkSnapshot cs = null;
            if (chunk.UpdateList.Count > 0)
            {
                cs = new()
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

            if (cs == null)
            {
                cs = new()
                {
                    Version = World.Service.TickTimes,
                    X = chunk.coord.X,
                    Y = chunk.coord.Y,
                };
                var result = World.Service.EntityService.GetChunkMovedEntity(chunk.coord);
                if (result.Count > 0)
                {
                    GD.Print($"【服务端】获取了 {result.Count} 个实体更新");
                    cs.Entiydatas = result;
                }

                WorldSnapshot.chunks.Add(cs);
            }
        }
    }
}