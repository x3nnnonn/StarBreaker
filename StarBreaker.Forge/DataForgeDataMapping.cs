using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeDataMapping
{
    public readonly uint StructCount;
    public readonly int StructIndex;
}