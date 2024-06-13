using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeEnumDefinition
{
    public readonly DataForgeStringId NameOffset;
    public readonly ushort ValueCount;
    public readonly ushort FirstValueIndex;
}