using System;
using Godot;
using Horizoncraft.script.Components.BlockComponents;
using Horizoncraft.script.Events;
using Horizoncraft.script.Services.chunk;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Components.Systems.BlockSystems;

public class ExplosiveSystem : TickSystem
{
    public override void BlockTick(BlockTickEvent blockTickEvent, Component component)
    {
        if (component is not ExplosiveComponent fuse) return;

        fuse.Fuse++;
        if (fuse.Fuse < fuse.FuseTime) return;

        var chunkService = blockTickEvent.Service.ChunkService;
        var globalPos = blockTickEvent.GlobalePos;

        int chunkRange = (int)MathF.Ceiling((float)fuse.Power / Chunk.Size);
        var centerChunk = new Vector2I(
            globalPos.X >= 0 ? globalPos.X / Chunk.Size : (globalPos.X - Chunk.Size + 1) / Chunk.Size,
            globalPos.Y >= 0 ? globalPos.Y / Chunk.Size : (globalPos.Y - Chunk.Size + 1) / Chunk.Size
        );

        if (!chunkService.EnsureChunksLoaded(centerChunk, chunkRange))
        {
            fuse.Fuse = fuse.FuseTime - 1;
            return;
        }

        blockTickEvent.SetBlock(Materials.Valueof("air"));
        Explode(blockTickEvent, chunkService, globalPos, fuse.Power);
    }

    private void Explode(BlockTickEvent e, ChunkServiceBase chunkService, Vector3I center, int power)
    {
        for (int x = -power; x <= power; x++)
        {
            for (int y = -power; y <= power; y++)
            {
                double dist = Math.Sqrt(x * x + y * y);
                if (dist > power) continue;

                var pos = new Vector3I(center.X + x, center.Y + y, 1);
                var block = chunkService.GetBlock(pos);
                if (block == null) continue;
                if (block.IsMeta("air")) continue;

                block.DropBlockLoot(e.World, new Vector2I(pos.X, pos.Y));
                chunkService.SetBlock(pos, Materials.Valueof("air"));
            }
        }
    }
}
