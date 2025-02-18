using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.Chf;

[DebuggerDisplay("{HeadId} {Percent}")]
public sealed class DnaPart
{
    public required byte HeadId { get; init; }
    public required float Percent { get; init; }

    public static DnaPart Read(ref SpanReader reader)
    {
        var value = reader.Read<ushort>();
        var headId = reader.Read<byte>();
        reader.Expect<byte>(0);

        return new DnaPart
        {
            Percent = value / (float)ushort.MaxValue * 100f,
            HeadId = headId
        };
    }
}