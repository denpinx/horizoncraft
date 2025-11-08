using System.IO;
using System.IO.Compression;
using MemoryPack;

namespace Horizoncraft.script.Net;
/// <summary>
/// 封装的MemoryPack序列化+Gzip压缩工具。
/// </summary>
public static class ByteTool
{
    public static byte[] ToBytes<T>(T t)
    {
        var bytes = MemoryPackSerializer.Serialize(t);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    public static T FromBytes<T>(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return MemoryPackSerializer.Deserialize<T>(output.ToArray());
    }
}