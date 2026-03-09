namespace Horizoncraft.script.WorldControl.Struct.structs;

/// <summary>
/// 静态建筑结构类型
/// </summary>
public class StaticBuildStruct : BlockStruct
{
    public string Name;
    /// <summary>
    /// 建筑结构的坐标重新转换
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
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