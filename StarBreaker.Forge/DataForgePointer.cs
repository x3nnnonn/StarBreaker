using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgePointer
{
    public readonly uint StructIndex;
    public readonly uint InstanceIndex;
}