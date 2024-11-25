using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct LocalFileHeader
{
    public readonly ushort VersionNeededToExtract;
    public readonly ushort GeneralPurposeBitFlag;
    public readonly ushort CompressionMethod;
    public readonly ushort LastModFileTime;
    public readonly ushort LastModFileDate;
    public readonly uint Crc32;
    public readonly uint CompressedSize;
    public readonly uint UncompressedSize;
    public readonly ushort FileNameLength;
    public readonly ushort ExtraFieldLength;
}