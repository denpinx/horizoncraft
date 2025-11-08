using System;
using Horizoncraft.script.WorldControl;
using MemoryPack;

namespace Horizoncraft.script.Net;

[MemoryPackable]
public partial class PlayerDataSnapshot
{
    public String Name;
    public PlayerState State;
    public float X;
    public float Y;
    public bool FaceLeft = false;
    public string HandItemId;

    [MemoryPackConstructor]
    public PlayerDataSnapshot()
    {
    }

    public PlayerDataSnapshot(PlayerData playerData)
    {
        Name = playerData.Name;
        State = playerData.State;
        X = playerData.Position.X;
        Y = playerData.Position.Y;
        FaceLeft = playerData.FaceLeft;
        var item = playerData.Inventory.GetHandItemStack();
        if (item != null)
        {
            HandItemId = item.Name;
        }
    }

    public System.Numerics.Vector2 GetVector2()
    {
        return new(X, Y);
    }
}