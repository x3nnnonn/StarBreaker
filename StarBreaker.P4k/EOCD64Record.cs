using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

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