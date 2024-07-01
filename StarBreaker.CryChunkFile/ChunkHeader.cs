using System.Runtime.InteropServices;

namespace StarBreaker.CryChunkFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ChunkHeader
{
    public readonly ChunkType ChunkType;
    public readonly uint Version;
    public readonly ulong Offset;
}