using System.Text;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class MaterialNameChunk : IChunk
{
    public string Name { get; private set; }

    public static IChunk Read(ref SpanReader reader)
    {
        var chunk = new MaterialNameChunk();
        var bytes = reader.ReadBytes(128);
        
        if (reader.ReadUInt32() != 0xdeadbeef)
            throw new Exception("Invalid terminator");
        if (reader.ReadUInt32() != 0x0000000)
            throw new Exception("Invalid terminator");
        
        var length = bytes.IndexOf((byte)0);
        chunk.Name = Encoding.ASCII.GetString(bytes[..length]);
        return chunk;
    }
}