using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeStructDefinition
{
    private static readonly ConcurrentDictionary<DataForgeStructDefinition, DataForgePropertyDefinition[]> _propertiesCache = new();
    private readonly DataForgeStringId2 NameOffset;
    public readonly uint ParentTypeIndex;
    public readonly ushort AttributeCount;
    public readonly ushort FirstAttributeIndex;
    public readonly uint NodeType;

    public string GetName(Database db) => db.GetString2(NameOffset);

    
    public int CalculateSize(ReadOnlySpan<DataForgeStructDefinition> structs, ReadOnlySpan<DataForgePropertyDefinition> properties)
    {
        var size = 0;

        foreach (var attribute in EnumerateProperties(structs, properties))
        {
            if (attribute.ConversionType != ConversionType.Attribute)
            {
                //array count + array offset
                size += sizeof(int) * 2;
                continue;
            }

            size += attribute.DataType switch
            {
                DataType.Reference => Unsafe.SizeOf<DataForgeReference>(),
                DataType.WeakPointer => Unsafe.SizeOf<uint>() * 2,
                DataType.StrongPointer => Unsafe.SizeOf<uint>() * 2,
                DataType.EnumChoice => Unsafe.SizeOf<DataForgeStringId>(),
                DataType.Guid => Unsafe.SizeOf<CigGuid>(),
                DataType.Locale => Unsafe.SizeOf<DataForgeStringId>(),
                DataType.Double => Unsafe.SizeOf<double>(),
                DataType.Single => Unsafe.SizeOf<float>(),
                DataType.String => Unsafe.SizeOf<DataForgeStringId>(),
                DataType.UInt64 => Unsafe.SizeOf<ulong>(),
                DataType.UInt32 => Unsafe.SizeOf<uint>(),
                DataType.UInt16 => Unsafe.SizeOf<ushort>(),
                DataType.Byte => Unsafe.SizeOf<byte>(),
                DataType.Int64 => Unsafe.SizeOf<long>(),
                DataType.Int32 => Unsafe.SizeOf<int>(),
                DataType.Int16 => Unsafe.SizeOf<short>(),
                DataType.SByte => Unsafe.SizeOf<sbyte>(),
                DataType.Boolean => Unsafe.SizeOf<byte>(),
                DataType.Class => structs[attribute.StructIndex].CalculateSize(structs, properties),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return size;
    }

    public DataForgePropertyDefinition[] EnumerateProperties(
        ReadOnlySpan<DataForgeStructDefinition> structs,
        ReadOnlySpan<DataForgePropertyDefinition> properties
    )
    {
        if (_propertiesCache.TryGetValue(this, out var cachedProperties))
            return cachedProperties;

        var _properties = new List<DataForgePropertyDefinition>();
        _properties.AddRange(properties.Slice(FirstAttributeIndex, AttributeCount));

        var baseStruct = this;
        while (baseStruct.ParentTypeIndex != 0xFFFFFFFF)
        {
            baseStruct = structs[(int)baseStruct.ParentTypeIndex];
            _properties.InsertRange(0, properties.Slice(baseStruct.FirstAttributeIndex, baseStruct.AttributeCount));
        }

        var arr = _properties.ToArray();
        _propertiesCache.TryAdd(this, arr);

        return arr;
    }

#if DEBUG
    public DataForgePropertyDefinition[] Properties => EnumerateProperties(DebugGlobal.Database.StructDefinitions, DebugGlobal.Database.PropertyDefinitions);
    public DataForgeStructDefinition? Parent => ParentTypeIndex == 0xffffffff ? null : DebugGlobal.Database.StructDefinitions[(int)ParentTypeIndex];
#endif
}