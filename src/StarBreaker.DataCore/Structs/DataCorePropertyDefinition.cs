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

#if DEBUG
    public string Name => DebugGlobal.Database.GetString2(NameOffset);
#endif
}