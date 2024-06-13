using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeRecord
{
    public readonly DataForgeStringId NameOffset;
    public readonly DataForgeStringId FileNameOffset;
    public readonly int StructIndex;
    public readonly CigGuid Hash;
    public readonly ushort InstanceIndex;
    public readonly ushort OtherIndex;
}