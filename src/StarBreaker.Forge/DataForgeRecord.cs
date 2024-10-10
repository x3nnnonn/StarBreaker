using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeRecord
{
    private readonly DataForgeStringId NameOffset;
    public readonly DataForgeStringId FileNameOffset;
    public readonly int StructIndex;
    public readonly CigGuid Hash;
    public readonly ushort InstanceIndex;
    public readonly ushort OtherIndex;
    
    public string GetName(Database db) => db.GetString2(NameOffset);
    
#if DEBUG
    public DataForgeStructDefinition Struct => DebugGlobal.Database.StructDefinitions[(int)StructIndex];
#endif
}