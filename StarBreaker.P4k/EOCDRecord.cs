using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EOCDRecord
{
    public static ReadOnlySpan<byte> Magic => [0x50, 0x4b, 0x05, 0x06 ];

    public readonly uint Signature;
    public readonly ushort DiskNumber;
    public readonly ushort StartDiskNumber;
    public readonly ushort EntriesOnDisk;
    public readonly ushort TotalEntries;
    public readonly uint CentralDirectorySize;
    public readonly uint CentralDirectoryOffset;
    public readonly ushort CommentLength;
    
    public bool IsZip64 => DiskNumber == 0xFFFF || 
                           StartDiskNumber == 0xFFFF || 
                           EntriesOnDisk == 0xFFFF || 
                           TotalEntries == 0xFFFF || 
                           CentralDirectorySize == 0xFFFFFFFF || 
                           CentralDirectoryOffset == 0xFFFFFFFF;
}

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Zip64Locator
{
    public static ReadOnlySpan<byte> Magic => [ 0x50, 0x4b, 0x06, 0x07 ];

    public readonly uint Signature;
    public readonly uint DiskWithZip64EOCD;
    public readonly ulong Zip64EOCDOffset;
    public readonly uint TotalDisks;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EOCD64Record
{
    public static ReadOnlySpan<byte> Magic => [ 0x50, 0x4b, 0x06, 0x06 ];

    public readonly uint Signature;
    public readonly ulong SizeOfRecord;
    public readonly ushort VersionMadeBy;
    public readonly ushort VersionNeededToExtract;
    public readonly uint DiskNumber;
    public readonly uint StartDiskNumber;
    public readonly ulong EntriesOnDisk;
    public readonly ulong TotalEntries;
    public readonly ulong CentralDirectorySize;
    public readonly ulong CentralDirectoryOffset;
}
