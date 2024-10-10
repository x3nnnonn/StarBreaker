using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeEnumDefinition
{
    private readonly DataForgeStringId2 NameOffset;
    public readonly ushort ValueCount;
    public readonly ushort FirstValueIndex;
    
    public string GetName(Database db) => db.GetString2(NameOffset);
}