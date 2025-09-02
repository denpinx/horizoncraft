using System;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class PlayerdataSnapshot
{
    public String Name;
    public float X;
    public float Y;
    public bool FaceLeft = false;
    public int HandItemId;

    [MemoryPackConstructor]
    public PlayerdataSnapshot()
    {
    }

    public PlayerdataSnapshot(PlayerData playerData)
    {
        Name = playerData.Name;
        X = playerData.Position.X;
        Y = playerData.Position.Y;
        FaceLeft = playerData.FaceLeft;
        var item = playerData.Inventory.GetHandItemStack();
        if (item != null)
        {
            HandItemId = item.Id;
        }
    }
}