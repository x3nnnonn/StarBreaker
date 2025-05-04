using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

internal class ExportFlagsChunk : IChunk
{
    public uint Skip { get; init; }
    public uint Major { get; init; }
    public uint Minor { get; init; }
    public uint Build { get; init; }
    public uint Revision { get; init; }
    public string S { get; init; }

    public static IChunk Read(ref SpanReader reader)
    {
        var skip = reader.ReadUInt32();
        var major = reader.ReadUInt32();
        var minor = reader.ReadUInt32();
        var build = reader.ReadUInt32();
        var revision = reader.ReadUInt32();
        var s = reader.ReadStringOfLength(144);
        return new ExportFlagsChunk()
        {
            Skip = skip,
            Major = major,
            Minor = minor,
            Build = build,
            Revision = revision,
            S = s
        };
    }
}