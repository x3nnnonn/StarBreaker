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