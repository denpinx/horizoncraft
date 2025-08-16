using System.IO;
using System.IO.Compression;
using Godot;
using MemoryPack;

namespace horizoncraft.script.WorldControl.Tool;

public static class WorldTool
{
    public static byte[] ToByte(this Chunk chunk)
    {
        var bytes = MemoryPackSerializer.Serialize<Chunk>(chunk);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        var outputarry = output.ToArray();
        //GD.Print("数据长度:"+outputarry.Length);
        return outputarry;
    }
    public static byte[] ToByte(this PlayerData playerdata)
    {
        byte[] bytes = MemoryPackSerializer.Serialize<PlayerData>(playerdata);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }
}