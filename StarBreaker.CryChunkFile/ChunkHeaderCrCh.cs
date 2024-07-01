using System.Runtime.InteropServices;

namespace StarBreaker.CryChunkFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ChunkHeaderCrCh
{
    public readonly ChunkTypeChCf ChunkType;
    private readonly ushort VersionRaw;
    public readonly int Id;
    public readonly uint Size;
    public readonly uint Offset;
    
    public bool IsBigEndian => (VersionRaw & 0x80000000u) != 0;
    public uint Version => VersionRaw & 0x7fffffffu;
}