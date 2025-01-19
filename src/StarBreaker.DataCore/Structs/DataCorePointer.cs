using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCorePointer
{
    public readonly int StructIndex;
    public readonly int InstanceIndex;
}