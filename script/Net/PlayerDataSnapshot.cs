using System;
using Godot;
using MemoryPack;

namespace horizoncraft.script.Net;

[MemoryPackable]
public partial class PlayerDataSnapshot
{
    public String Name;
    public float X;
    public float Y;
    public bool FaceLeft = false;
    public int HandItemId;

    [MemoryPackConstructor]
    public PlayerDataSnapshot()
    {
    }

    public PlayerDataSnapshot(PlayerData playerData)
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

    public System.Numerics.Vector2 GetVector2()
    {
        return new(X, Y);
    }
}