using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct CentralDirectoryFileHeader
{
    public static ReadOnlySpan<byte> Magic => [ 0x50, 0x4b, 0x01, 0x02 ];
    
    public readonly uint Signature;
    public readonly ushort VersionMadeBy;
    public readonly ushort VersionNeededToExtract;
    public readonly ushort GeneralPurposeBitFlag;
    public readonly ushort CompressionMethod;
    public readonly ushort LastModifiedTime;
    public readonly ushort LastModifiedDate;
    public readonly uint Crc32;
    public readonly uint CompressedSize;
    public readonly uint UncompressedSize;
    public readonly ushort FileNameLength;
    public readonly ushort ExtraFieldLength;
    public readonly ushort FileCommentLength;
    public readonly ushort DiskNumberStart;
    public readonly ushort InternalFileAttributes;
    public readonly uint ExternalFileAttributes;
    public readonly uint LocalFileHeaderOffset;
}