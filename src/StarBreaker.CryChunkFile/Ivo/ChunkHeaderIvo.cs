using System.Runtime.InteropServices;

namespace StarBreaker.CryChunkFile;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ChunkHeaderIvo
{
    public readonly ChunkTypeIvo ChunkType;
    public readonly uint Version;
    public readonly int Offset;
}