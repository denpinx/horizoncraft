using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Utility;
using Horizoncraft.script.Net;
using Horizoncraft.script.rpc;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.chunk;

public class ClientChunkService(
    World world,
    NeoWorldGenerator worldGenerator
) :
    ChunkServiceBase(world, worldGenerator)
{
    private ConcurrentDictionary<Vector2I, int> RegetList = new();


    protected override async Task<Chunk> LoadChunk(Vector2I pos)
    {
        if (RegetList.TryGetValue(pos, out int i))
        {
            if (i > 5)
            {
            }
            else
            {
                RegetList[pos] = i + 1;
            }
        }
        else
        {
            RegetList.TryAdd(new Vector2I(pos.X, pos.Y), 1);
        }

        return null;
    }

    public override void Ticking()
    {
        UpdateLights();
        AsyncOwnedEntities();
        foreach (var pos in RegetList.Keys.ToArray())
        {
            var i = RegetList[pos];
            if (Chunks.ContainsKey(pos))
            {
                RegetList.TryRemove(pos, out i);
                continue;
            }

            if (i > 5)
            {
                RegetList.TryRemove(pos, out i);
                World.Service.ChunkServiceNode.RpcId(1,
                    nameof(ChunkServiceNode.ReGetChunk), pos.X, pos.Y
                );
                //GameLogger.Info("Client",$"重新加载区块{pos}");
            }
        }
    }

    public void AsyncOwnedEntities()
    {
        EntityPack entityPack = new EntityPack();
        entityPack.From = PlayerNode.Profile.Name;
        foreach (var entity in World.Service.EntityService.EntityDatas.Values)
        {
            if (entity.Owned == PlayerNode.Profile.Name)
            {
                if (entity.Update)
                {
                    entity.Update = false;
                    entityPack.Entitys.Add(new EntityDataSnapShot(entity));
                }
            }
        }

        if (entityPack.Entitys.Count > 0)
        {
            GameLogger.Info("Client",$"[客户端]同步{entityPack.Entitys.Count}个实体回服务端");
            World.Service.EntityServiceNode.RpcId(1,
                nameof(EntityServiceNode.ServerReceiveEntityPack),
                ByteTool.ToBytes(entityPack));
        }
    }

    public override void SaveChunk(Chunk chunk)
    {
    }

    public override void SaveAll()
    {
    }
}