using System.Threading.Tasks;
using Godot;
using Horizoncraft.script;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.chunk;

public class PreviewChunkService : ChunkServiceBase
{
    public PreviewChunkService(World world) : base(world)
    {
    }

    protected override async Task<Chunk> LoadChunk(Vector2I pos)
    {
        var chunk = new Chunk(pos.X, pos.Y);
        World.Service.NeoWorldGenerator.Generator(chunk);
        return chunk;
    }

    public override void SaveChunk(Chunk chunk)
    {
    }

    public override void SaveAll()
    {
    }
}  