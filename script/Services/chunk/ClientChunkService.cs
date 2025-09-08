using System.Collections.Concurrent;
using System.Linq;
using Godot;
using horizoncraft.script;
using horizoncraft.script.rpc;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.chunk;

public class ClientChunkService : ChunkServiceBase
{
    private ConcurrentDictionary<Vector2I, int> RegetList = new();

    public ClientChunkService(World world) : base(world)
    {
    }

    public override Chunk LoadChunk(Vector2I pos)
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
        base.Ticking();
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
                _world.Service.ChunkServiceNode.RpcId(1,
                    nameof(ChunkServiceNode.ReGetChunk), pos.X, pos.Y
                );
                //GD.Print($"重新加载区块{pos}");
            }
        }
    }

    public override void SaveChunk(Chunk chunk)
    {
    }

    public override void SaveAll()
    {
    }
}