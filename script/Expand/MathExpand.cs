using Godot;
using Vector2 = System.Numerics.Vector2;

namespace horizoncraft.script.Expand;

public static class MathExpand
{
    /// <summary>
    /// godot向量转c#向量
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Vector2 ToSystemVector2(this Godot.Vector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    /// <summary>
    /// godot向量转Godot整数向量
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Godot.Vector2I ToVector2I(this Godot.Vector2 vector)
    {
        return (Godot.Vector2I)vector;
    }

    /// <summary>
    /// C# 向量 转 Godot整数向量
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Godot.Vector2I ToVector2I(this Vector2 vector)
    {
        return new Vector2I((int)vector.X, (int)vector.Y);
    }

    /// <summary>
    /// C# 向量转 Godot向量
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Godot.Vector2 ToGodotVector2(this Vector2 vector)
    {
        return new Godot.Vector2(vector.X, vector.Y);
    }

    public static Vector2I MathFloor(this Vector2I V2I, int chunkSize)
    {
        return new Vector2I(
            (int)Mathf.Floor((float)V2I.X / chunkSize),
            (int)Mathf.Floor((float)V2I.Y / chunkSize)
        );
    }

    public static Vector2I MathFloor(this Vector3I v3i, int chunkSize)
    {
        return new Vector2I(
            (int)Mathf.Floor((float)v3i.X / chunkSize),
            (int)Mathf.Floor((float)v3i.Y / chunkSize)
        );
    }

    public static Vector2I MathFloor(this Vector2 vector2, int chunkSize)
    {
        return new Vector2I(
            (int)Mathf.Floor((float)vector2.X / chunkSize),
            (int)Mathf.Floor((float)vector2.Y / chunkSize)
        );
    }

    public static Vector2I Remainder(this Vector3I V3I, int chunkSize)
    {
        return new Vector2I(
            (V3I.X % chunkSize + chunkSize) % chunkSize,
            (V3I.Y % chunkSize + chunkSize) % chunkSize
        );
    }

    public static Vector2I Remainder(this Vector2I v2i, int chunkSize)
    {
        return new Vector2I(
            (v2i.X % chunkSize + chunkSize) % chunkSize,
            (v2i.Y % chunkSize + chunkSize) % chunkSize
        );
    }

    public static Vector3I ToVector3I(this System.Numerics.Vector3 v3)
    {
        return new Vector3I((int)v3.X, (int)v3.Y, (int)v3.Z);
    }
}