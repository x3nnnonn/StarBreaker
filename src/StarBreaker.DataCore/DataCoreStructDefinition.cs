using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreStructDefinition
{
    private static readonly ConcurrentDictionary<DataCoreStructDefinition, DataCorePropertyDefinition[]> _propertiesCache = new();
    private readonly DataCoreStringId2 NameOffset;
    public readonly uint ParentTypeIndex;
    public readonly ushort AttributeCount;
    public readonly ushort FirstAttributeIndex;
    public readonly uint NodeType;

    public string GetName(DataCoreDatabase db) => db.GetString2(NameOffset);

#if DEBUG
    public DataCoreStructDefinition? Parent => ParentTypeIndex == 0xffffffff ? null : DebugGlobal.Database.StructDefinitions[(int)ParentTypeIndex];
    public string Name => GetName(DebugGlobal.Database);
#endif
}