using System;
using System.Collections.Generic;
using Godot;
using horizoncraft.script;
using horizoncraft.script.Net;
using horizoncraft.script.rpc;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.chunk;

public class HostChunkService : ChunkServiceBase
{
    public WorldSnapshot WorldSnapshot = new WorldSnapshot();

    public HostChunkService(World world) : base(world)
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
            foreach (var playerset in _world.Service.PlayerService.Players)
            {
                PlayerData pd1 = playerset.Value;
                //按距离同步
                if (
                    Math.Abs(chunk.X - pd1.ChunkCoord.X) <= _loadrange &&
                    Math.Abs(chunk.Y - pd1.ChunkCoord.Y) <= _loadrange
                )
                {
                    if (!diffUpdate.ContainsKey(pd1.PeerId))
                        diffUpdate[pd1.PeerId] = new WorldSnapshot();
                    diffUpdate[pd1.PeerId].chunks.Add(chunk);
                    chunk.ResetEmptyOwned(pd1.Name); //更新实体的从属
                }
            }
        }

        //全量更新
        foreach (var chunk in Chunks.Values)
            if (chunk.update_server)
            {
                //下一帧同步这个区块内的玩家
                _world.Service.PlayerService.ResetPlayerMoveStateByChunk(chunk.coord);
                foreach (var playerset in _world.Service.PlayerService.Players)
                {
                    PlayerData pd1 = playerset.Value;
                    //按距离同步
                    if (
                        Math.Abs(chunk.X - pd1.ChunkCoord.X) <= _loadrange &&
                        Math.Abs(chunk.Y - pd1.ChunkCoord.Y) <= _loadrange
                    )
                    {
                        if (pd1.Name != PlayerNode.Profile.Name)
                        {
                            if (!wholeChunkUpdate.ContainsKey(pd1.PeerId))
                            {
                                var result = _world.Service.EntityService.GetEntityByChunk(chunk.coord);
                                chunk.Entitys = result;
                                wholeChunkUpdate[pd1.PeerId] = new ChunkPack();
                            }

                            wholeChunkUpdate[pd1.PeerId].Chunks.Add(chunk);
                        }
                    }
                }
            }
        
        foreach (var chunk in Chunks.Values)
            chunk.update_server = false;
        foreach (var key in wholeChunkUpdate.Keys)
        {
            var bytes = ByteTool.ToBytes<ChunkPack>(wholeChunkUpdate[key]);
            _world.Service.ChunkServiceNode.RpcId(key,
                nameof(ChunkServiceNode.ReciveChunkPack),
                bytes);
        }

        foreach (var key in diffUpdate.Keys)
        {
            if (key != 0)
            {
                var bytes = ByteTool.ToBytes<WorldSnapshot>(diffUpdate[key]);
                _world.Service.ChunkServiceNode.RpcId(key,
                    nameof(ChunkServiceNode.ReciveChunkUpdatePack),
                    bytes);
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
                    Version = _world.Service.TickTimes,
                    X = chunk.coord.X,
                    Y = chunk.coord.Y,
                };

                foreach (var v in chunk.UpdateList)
                {
                    cs.list.Add(new BlockSnapshot()
                    {
                        x = (byte)v.X,
                        y = (byte)v.Y,
                        z = (byte)v.Z,
                        id = (short)chunk.GetBlock(v.X, v.Y, v.Z).Id,
                        state = (byte)chunk.GetBlock(v.X, v.Y, v.Z).State
                    });
                }

                WorldSnapshot.chunks.Add(cs);
            }

            if (cs == null)
            {
                cs = new()
                {
                    Version = _world.Service.TickTimes,
                    X = chunk.coord.X,
                    Y = chunk.coord.Y,
                };
                var result = _world.Service.EntityService.GetChunkMovedEntity(chunk.coord);
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