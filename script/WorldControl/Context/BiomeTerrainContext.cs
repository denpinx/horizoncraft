using System;
using Horizoncraft.script;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.WorldControl.Context;

public class BiomeTerrainContext
{
    public required NeoMaterials NeoMaterials;
    public Chunk Chunk;
    public int[,] HighMap;
    public Random Random;
    public float Noise;
    public int LocalX;
    public int LocalY;
    public int GlobalX;
    public int GlobalY;
    public int GlobalZ;
    public BlockData BlockData;


    public void SetBlock(string name, int id = 0)
    {
        Chunk.SetBlock(LocalX, LocalY, GlobalZ, Materials.Valueof(name), id);
    }
}