using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCorePropertyDefinition
{
    private readonly DataCoreStringId2 NameOffset;
    public readonly ushort StructIndex;
    public readonly DataType DataType;
    public readonly ConversionType ConversionType;
    private readonly ushort _padding;
    
    public string GetName(DataCoreDatabase db) => db.GetString2(NameOffset);

    public bool IsAttribute => ConversionType == ConversionType.Attribute && 
                               DataType != DataType.Class && 
                               DataType != DataType.StrongPointer;
    
#if DEBUG
    public string Name => DebugGlobal.Database.GetString2(NameOffset);
    public DataCoreStructDefinition? Struct => IsAttribute 
        ? null 
        : DebugGlobal.Database.StructDefinitions[StructIndex];
#endif
}