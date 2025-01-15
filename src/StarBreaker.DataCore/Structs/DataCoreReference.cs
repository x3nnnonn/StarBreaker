using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreReference
{
    public readonly uint InstanceIndex;
    public readonly CigGuid RecordId;

    public bool IsInvalid => RecordId == CigGuid.Empty || InstanceIndex == 0xFFFFFFFF;
}