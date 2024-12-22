using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreEnumDefinition
{
    private readonly DataCoreStringId2 NameOffset;
    public readonly ushort ValueCount;
    public readonly ushort FirstValueIndex;
    
    public string GetName(DataCoreDatabase db) => db.GetString2(NameOffset);

    #if DEBUG
    public string Name => DebugGlobal.Database.GetString2(NameOffset);
    public string Values => string.Join(", ", DebugGlobal.Database.EnumOptions[FirstValueIndex..(FirstValueIndex + ValueCount)]);
    #endif
}