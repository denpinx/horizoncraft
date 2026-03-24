using System.Threading.Tasks;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.chunk;

public class PreviewChunkService(World world,NeoWorldGenerator worldGenerator) : ChunkServiceBase(world,worldGenerator)
{

    protected override async Task<Chunk> LoadChunk(Vector2I pos)
    {
        var chunk = new Chunk(pos.X, pos.Y);
        worldGenerator.Generator(chunk);
        return chunk;
    }

    public override void SaveChunk(Chunk chunk)
    {
    }

    public override void SaveAll()
    {
    }
}  