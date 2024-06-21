namespace StarBreaker.P4k;

public sealed class MyZipEntry
{
    public string Name { get; }
    public string Comment { get; }
    public ulong CompressedSize { get; }
    public ulong UncompressedSize { get; }
    public ushort CompressionMethod { get; }
    public bool IsCrypted { get; }
    public ulong Offset { get; }
    
    public MyZipEntry(string name, string comment, ulong compressedSize, ulong uncompressedSize, ushort compressionMethod, bool isCrypted, ulong offset)
    {
        Name = name;
        Comment = comment;
        CompressedSize = compressedSize;
        UncompressedSize = uncompressedSize;
        CompressionMethod = compressionMethod;
        IsCrypted = isCrypted;
        Offset = offset;
    }
}