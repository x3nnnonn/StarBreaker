using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgePropertyDefinition
{
    private readonly DataForgeStringId2 NameOffset;
    public readonly ushort StructIndex;
    public readonly DataType DataType;
    public readonly ConversionType ConversionType;
    private readonly ushort _padding;
    
    public string GetName(Database db) => db.GetString2(NameOffset);

    public bool IsAttribute => ConversionType == ConversionType.Attribute && 
                               DataType != DataType.Class && 
                               DataType != DataType.StrongPointer;
    
#if DEBUG
    public DataForgeStructDefinition? Struct => IsAttribute ? null :
        DebugGlobal.Database.StructDefinitions[(int)StructIndex];

    public object? Value => !IsAttribute
        ? null
        : DataType switch
        {
            _ => "test"
            
        };
#endif
}