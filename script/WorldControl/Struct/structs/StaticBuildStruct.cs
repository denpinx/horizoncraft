using System.Reflection.Metadata;
using Godot;

namespace horizoncraft.script.WorldControl.Struct.structs;

public class StaticBuildStruct : BlockStruct
{
    public string Name;

    public BlockStruct GetBlockStruct(int x, int y, int z)
    {
        BlockStruct blockStruct = new BlockStruct();
        foreach (var block in blockStructItems)
        {
            blockStruct.AddBlock(block.Value.Coord.X + x, block.Value.Coord.Y + y, block.Value.Coord.Z + z,
                block.Value.BlockMeta);
        }

        return blockStruct;
    }
}