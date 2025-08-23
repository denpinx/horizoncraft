using System;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class PlayerdataSnapshot
{
    public String Name;
    public float x;
    public float y;
    
    public static PlayerdataSnapshot ToSnapshot(PlayerData pd)
    {
        return new PlayerdataSnapshot()
        {
            Name = pd.Name,
            x = pd.Position.X,
            y = pd.Position.Y,
        };
    }
}