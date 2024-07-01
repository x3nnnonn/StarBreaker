using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgePropertyDefinition
{
    public readonly DataForgeStringId NameOffset;
    public readonly ushort StructIndex;
    public readonly DataType DataType;
    public readonly ConversionType ConversionType;
    private readonly ushort _padding;

    public bool IsAttribute => ConversionType == ConversionType.Attribute && 
                               DataType != DataType.Class && 
                               DataType != DataType.StrongPointer;
}