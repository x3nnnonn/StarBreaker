using StarBreaker.Common;
using ZstdSharp;

namespace StarBreaker.P4k;

public sealed class ZipEntry
{
    private readonly P4kFile _parent;
    public string Name { get; }
    public ulong CompressedSize { get; }
    public ulong UncompressedSize { get; }
    public ushort CompressionMethod { get; }
    public bool IsCrypted { get; }
    public ulong Offset { get; }
    public DateTime LastModified { get; }

    public ZipEntry(P4kFile parent,
        string name,
        ulong compressedSize,
        ulong uncompressedSize,
        ushort compressionMethod,
        bool isCrypted,
        ulong offset,
        ushort lastModifiedTime,
        ushort lastModifiedDate
    )
    {
        _parent = parent;
        Name = name;
        CompressedSize = compressedSize;
        UncompressedSize = uncompressedSize;
        CompressionMethod = compressionMethod;
        IsCrypted = isCrypted;
        Offset = offset;

        var year = (lastModifiedDate >> 9) + 1980;
        var month = (lastModifiedDate >> 5) & 0xF;
        var day = lastModifiedDate & 0x1F;
        var hour = lastModifiedTime >> 11;
        var minute = (lastModifiedTime >> 5) & 0x3F;
        var second = (lastModifiedTime & 0x1F) * 2;

        LastModified = new DateTime(year, month, day, hour, minute, second);
    }

    public Stream Open() => _parent.Open(this);
}