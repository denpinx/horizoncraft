using System.Threading.Tasks;
using Godot;
using horizoncraft.script;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.chunk;

public class PreviewChunkService : ChunkServiceBase
{
    public PreviewChunkService(World world) : base(world)
    {
    }

    protected override async Task<Chunk> LoadChunk(Vector2I pos)
    {
        var chunk = new Chunk(pos.X, pos.Y);
        WorldGenerator.Generator(chunk);
        return chunk;
    }

    public override void SaveChunk(Chunk chunk)
    {
    }

    public override void SaveAll()
    {
    }
}