using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCorePointer
{
    public readonly int StructIndex;
    public readonly int InstanceIndex;

    public bool IsInvalid => StructIndex == -1 || InstanceIndex == -1;

    public override string ToString() => $"Pointer.{StructIndex}.{InstanceIndex}";
}