using System.Text;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class MaterialNameChunk : IChunk
{
    public required string Name { get; init; }

    public static IChunk Read(ref SpanReader reader)
    {
        var bytes = reader.ReadBytes(128);

        var length = bytes.IndexOf((byte)0);
        var name = Encoding.ASCII.GetString(bytes[..length]);

        //this is very often 0xdeadbeef, but not always
        _ = reader.ReadUInt32();
        // this one is lots of things?? maybe a bitfield? unknown.
        _ = reader.ReadUInt32();

        return new MaterialNameChunk
        {
            Name = name
        };
    }
}