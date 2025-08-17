using System.IO;
using System.IO.Compression;
using MemoryPack;

namespace horizoncraft.script.Net;

public class AsByteable<T>
{
    public static byte[] ToBytes(T t)
    {
        var bytes = MemoryPackSerializer.Serialize(t);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    public static T FromBytes(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return MemoryPackSerializer.Deserialize<T>(output.ToArray());
    }
}