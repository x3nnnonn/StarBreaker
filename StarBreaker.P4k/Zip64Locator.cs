using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Zip64Locator
{
    public static ReadOnlySpan<byte> Magic => [ 0x50, 0x4b, 0x06, 0x07 ];

    public readonly uint Signature;
    public readonly uint DiskWithZip64EOCD;
    public readonly ulong Zip64EOCDOffset;
    public readonly uint TotalDisks;
}