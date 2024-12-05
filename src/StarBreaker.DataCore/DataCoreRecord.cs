using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreRecord
{
    private readonly DataCoreStringId2 NameOffset;
    private readonly DataCoreStringId FileNameOffset;
    public readonly int StructIndex;
    public readonly CigGuid Hash;
    public readonly ushort InstanceIndex;
    public readonly ushort OtherIndex;
    
    public string GetName(DataCoreDatabase db) => db.GetString2(NameOffset);
    public string GetFileName(DataCoreDatabase db) => db.GetString(FileNameOffset);
    
#if DEBUG
    public DataCoreStructDefinition Struct => DebugGlobal.Database.StructDefinitions[(int)StructIndex];
#endif
}