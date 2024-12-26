using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCorePointer
{
    public readonly uint StructIndex;
    public readonly uint InstanceIndex;

    public bool IsInvalid => StructIndex == 0xFFFFFFFF || InstanceIndex == 0xFFFFFFFF;

    public override string ToString() => $"Pointer.{StructIndex}.{InstanceIndex}";
}