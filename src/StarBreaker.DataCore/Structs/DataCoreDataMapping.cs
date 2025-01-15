using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreDataMapping
{
    public readonly uint StructCount;
    public readonly int StructIndex;
}