namespace StarBreaker.P4k;

public sealed class ZipEntry
{
    public string Name { get; }
    public ulong CompressedSize { get; }
    public ulong UncompressedSize { get; }
    public ushort CompressionMethod { get; }
    public bool IsCrypted { get; }
    public ulong Offset { get; }
    public DateTime LastModified { get; }
    
    public ZipEntry(string name, ulong compressedSize, ulong uncompressedSize, ushort compressionMethod, bool isCrypted, ulong offset, ushort lastModifiedTime, ushort lastModifiedDate)
    {
        Name = name;
        CompressedSize = compressedSize;
        UncompressedSize = uncompressedSize;
        CompressionMethod = compressionMethod;
        IsCrypted = isCrypted;
        Offset = offset;
        LastModified = ParseDosDateTime(lastModifiedDate, lastModifiedTime);
    }

    private static DateTime ParseDosDateTime(ushort dosDate, ushort dosTime)
    {
        var year = (dosDate >> 9) + 1980;
        var month = (dosDate >> 5) & 0xF;
        var day = dosDate & 0x1F;
        var hour = dosTime >> 11;
        var minute = (dosTime >> 5) & 0x3F;
        var second = (dosTime & 0x1F) * 2;
        return new DateTime(year, month, day, hour, minute, second);
    }
}