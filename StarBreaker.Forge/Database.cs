using System.Collections.Frozen;
using System.Text;

namespace StarBreaker.Forge;

public class Database
{
    public readonly byte[] Bytes;

    public readonly ReadOnlyMemory<DataForgeStructDefinition> StructDefinitions;
    public readonly ReadOnlyMemory<DataForgePropertyDefinition> PropertyDefinitions;
    public readonly ReadOnlyMemory<DataForgeEnumDefinition> EnumDefinitions;
    public readonly ReadOnlyMemory<DataForgeDataMapping> DataMappings;
    public readonly ReadOnlyMemory<DataForgeRecord> RecordDefinitions;

    public readonly ReadOnlyMemory<sbyte> Int8Values;
    public readonly ReadOnlyMemory<short> Int16Values;
    public readonly ReadOnlyMemory<int> Int32Values;
    public readonly ReadOnlyMemory<long> Int64Values;

    public readonly ReadOnlyMemory<byte> UInt8Values;
    public readonly ReadOnlyMemory<ushort> UInt16Values;
    public readonly ReadOnlyMemory<uint> UInt32Values;
    public readonly ReadOnlyMemory<ulong> UInt64Values;

    public readonly ReadOnlyMemory<bool> BooleanValues;
    public readonly ReadOnlyMemory<float> SingleValues;
    public readonly ReadOnlyMemory<double> DoubleValues;
    public readonly ReadOnlyMemory<CigGuid> GuidValues;

    public readonly ReadOnlyMemory<DataForgeStringId> StringIdValues;
    public readonly ReadOnlyMemory<DataForgeStringId> LocaleValues;
    public readonly ReadOnlyMemory<DataForgeStringId> EnumValues;

    public readonly ReadOnlyMemory<DataForgePointer> StrongValues;
    public readonly ReadOnlyMemory<DataForgePointer> WeakValues;
    public readonly ReadOnlyMemory<DataForgeReference> ReferenceValues;

    public readonly ReadOnlyMemory<DataForgeStringId> EnumOptions;

    public readonly DataForgeStringId ValueStringId;
    public readonly DataForgeStringId CountStringId;
    
    private readonly FrozenDictionary<DataType, DataForgeStringId> _dataTypeStringIds;
    private readonly FrozenDictionary<int, string> _cachedStringsss;

    public Database(byte[] bytes, out int bytesRead)
    {
        Bytes = bytes;
        var reader = new ArrayReader(bytes);
        _ = reader.Read<uint>();
        var fileVersion = reader.Read<uint>();
        _ = reader.Read<uint>();
        _ = reader.Read<uint>();

        var structDefinitionCount = reader.Read<int>();
        var propertyDefinitionCount = reader.Read<int>();
        var enumDefinitionCount = reader.Read<int>();
        var dataMappingCount = reader.Read<int>();
        var recordDefinitionCount = reader.Read<int>();
        var booleanValueCount = reader.Read<int>();
        var int8ValueCount = reader.Read<int>();
        var int16ValueCount = reader.Read<int>();
        var int32ValueCount = reader.Read<int>();
        var int64ValueCount = reader.Read<int>();
        var uint8ValueCount = reader.Read<int>();
        var uint16ValueCount = reader.Read<int>();
        var uint32ValueCount = reader.Read<int>();
        var uint64ValueCount = reader.Read<int>();
        var singleValueCount = reader.Read<int>();
        var doubleValueCount = reader.Read<int>();
        var guidValueCount = reader.Read<int>();
        var stringIdValueCount = reader.Read<int>();
        var localeValueCount = reader.Read<int>();
        var enumValueCount = reader.Read<int>();
        var strongValueCount = reader.Read<int>();
        var weakValueCount = reader.Read<int>();
        var referenceValueCount = reader.Read<int>();
        var enumOptionCount = reader.Read<int>();
        var textLength = reader.Read<uint>();
        _ = reader.Read<uint>();

        StructDefinitions = reader.GetMemory<DataForgeStructDefinition>(structDefinitionCount);
        PropertyDefinitions = reader.GetMemory<DataForgePropertyDefinition>(propertyDefinitionCount);
        EnumDefinitions = reader.GetMemory<DataForgeEnumDefinition>(enumDefinitionCount);
        DataMappings = reader.GetMemory<DataForgeDataMapping>(dataMappingCount);
        RecordDefinitions = reader.GetMemory<DataForgeRecord>(recordDefinitionCount);

        Int8Values = reader.GetMemory<sbyte>(int8ValueCount);
        Int16Values = reader.GetMemory<short>(int16ValueCount);
        Int32Values = reader.GetMemory<int>(int32ValueCount);
        Int64Values = reader.GetMemory<long>(int64ValueCount);

        UInt8Values = reader.GetMemory<byte>(uint8ValueCount);
        UInt16Values = reader.GetMemory<ushort>(uint16ValueCount);
        UInt32Values = reader.GetMemory<uint>(uint32ValueCount);
        UInt64Values = reader.GetMemory<ulong>(uint64ValueCount);

        BooleanValues = reader.GetMemory<bool>(booleanValueCount);
        SingleValues = reader.GetMemory<float>(singleValueCount);
        DoubleValues = reader.GetMemory<double>(doubleValueCount);
        GuidValues = reader.GetMemory<CigGuid>(guidValueCount);

        StringIdValues = reader.GetMemory<DataForgeStringId>(stringIdValueCount);
        LocaleValues = reader.GetMemory<DataForgeStringId>(localeValueCount);
        EnumValues = reader.GetMemory<DataForgeStringId>(enumValueCount);

        StrongValues = reader.GetMemory<DataForgePointer>(strongValueCount);
        WeakValues = reader.GetMemory<DataForgePointer>(weakValueCount);
        ReferenceValues = reader.GetMemory<DataForgeReference>(referenceValueCount);
        EnumOptions = reader.GetMemory<DataForgeStringId>(enumOptionCount);
        
        var stringSpan = reader.ReadSpan((int)textLength);

        var strings = new Dictionary<int, string>();
        var offset = 0;
        while (offset < stringSpan.Length)
        {
            var length = stringSpan[offset..].IndexOf((byte)0);
            var useful = stringSpan[offset..(offset + length)];
            var str = Encoding.ASCII.GetString(useful);
            strings[offset] = str;
            offset += length + 1;
        }

        ValueStringId = ReserveStringId(strings, "__value");
        CountStringId = ReserveStringId(strings, "__count");
        
        var dataTypeStringIds = new Dictionary<DataType, DataForgeStringId>();
        foreach (var type in Enum.GetValues<DataType>())
        {
            var id = ReserveStringId(strings, type.ToString());
            dataTypeStringIds[type] = id;
        }

        _cachedStringsss = strings.ToFrozenDictionary();
        _dataTypeStringIds = dataTypeStringIds.ToFrozenDictionary();
        
        bytesRead = reader.Position;

#if DEBUG
        DebugGlobal.Database = this;
#endif
    }
    
    public string GetString(DataForgeStringId id) => _cachedStringsss[id.Id];
    public DataForgeStringId GetDataTypeStringId(DataType type) => _dataTypeStringIds[type];

    private int _stringCounter;
    private DataForgeStringId ReserveStringId(Dictionary<int, string> dict, string str)
    {
        var id = int.MaxValue - _stringCounter;
        
        ++_stringCounter;
        
        dict.Add(id, str);
        return new DataForgeStringId(id);
    }
}