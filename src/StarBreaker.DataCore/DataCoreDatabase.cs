using System.Collections.Frozen;
using System.Text;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public class DataCoreDatabase
{
    private readonly int DataSectionOffset;
    private readonly byte[] DataSection;

    public readonly DataCoreStructDefinition[] StructDefinitions;
    public readonly DataCorePropertyDefinition[] PropertyDefinitions;
    public readonly DataCoreEnumDefinition[] EnumDefinitions;
    public readonly DataCoreDataMapping[] DataMappings;
    public readonly DataCoreRecord[] RecordDefinitions;

    public readonly sbyte[] Int8Values;
    public readonly short[] Int16Values;
    public readonly int[] Int32Values;
    public readonly long[] Int64Values;

    public readonly byte[] UInt8Values;
    public readonly ushort[] UInt16Values;
    public readonly uint[] UInt32Values;
    public readonly ulong[] UInt64Values;

    public readonly bool[] BooleanValues;
    public readonly float[] SingleValues;
    public readonly double[] DoubleValues;
    public readonly CigGuid[] GuidValues;

    public readonly DataCoreStringId[] StringIdValues;
    public readonly DataCoreStringId[] LocaleValues;
    public readonly DataCoreStringId[] EnumValues;

    public readonly DataCorePointer[] StrongValues;
    public readonly DataCorePointer[] WeakValues;
    public readonly DataCoreReference[] ReferenceValues;

    public readonly DataCoreStringId2[] EnumOptions;
    
    public readonly FrozenDictionary<int, int[]> Offsets;

    public readonly FrozenDictionary<int, string> CachedStrings;
    public readonly FrozenDictionary<int, string> CachedStrings2;
    public readonly FrozenDictionary<CigGuid, DataCoreRecord> RecordMap;

    public DataCoreDatabase(Stream fs)
    {
        using var reader = new BinaryReader(fs);

        _ = reader.ReadUInt32();
        var fileVersion = reader.ReadUInt32();
        if (fileVersion != 6)
            throw new Exception($"Unsupported file version: {fileVersion}");
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();

        var structDefinitionCount = reader.ReadInt32();
        var propertyDefinitionCount = reader.ReadInt32();
        var enumDefinitionCount = reader.ReadInt32();
        var dataMappingCount = reader.ReadInt32();
        var recordDefinitionCount = reader.ReadInt32();
        var booleanValueCount = reader.ReadInt32();
        var int8ValueCount = reader.ReadInt32();
        var int16ValueCount = reader.ReadInt32();
        var int32ValueCount = reader.ReadInt32();
        var int64ValueCount = reader.ReadInt32();
        var uint8ValueCount = reader.ReadInt32();
        var uint16ValueCount = reader.ReadInt32();
        var uint32ValueCount = reader.ReadInt32();
        var uint64ValueCount = reader.ReadInt32();
        var singleValueCount = reader.ReadInt32();
        var doubleValueCount = reader.ReadInt32();
        var guidValueCount = reader.ReadInt32();
        var stringIdValueCount = reader.ReadInt32();
        var localeValueCount = reader.ReadInt32();
        var enumValueCount = reader.ReadInt32();
        var strongValueCount = reader.ReadInt32();
        var weakValueCount = reader.ReadInt32();
        var referenceValueCount = reader.ReadInt32();
        var enumOptionCount = reader.ReadInt32();
        var textLength = reader.ReadUInt32();
        var textLength2 = reader.ReadUInt32();

        StructDefinitions = reader.ReadArray<DataCoreStructDefinition>(structDefinitionCount);
        PropertyDefinitions = reader.ReadArray<DataCorePropertyDefinition>(propertyDefinitionCount);
        EnumDefinitions = reader.ReadArray<DataCoreEnumDefinition>(enumDefinitionCount);
        DataMappings = reader.ReadArray<DataCoreDataMapping>(dataMappingCount);
        RecordDefinitions = reader.ReadArray<DataCoreRecord>(recordDefinitionCount);

        Int8Values = reader.ReadArray<sbyte>(int8ValueCount);
        Int16Values = reader.ReadArray<short>(int16ValueCount);
        Int32Values = reader.ReadArray<int>(int32ValueCount);
        Int64Values = reader.ReadArray<long>(int64ValueCount);

        UInt8Values = reader.ReadArray<byte>(uint8ValueCount);
        UInt16Values = reader.ReadArray<ushort>(uint16ValueCount);
        UInt32Values = reader.ReadArray<uint>(uint32ValueCount);
        UInt64Values = reader.ReadArray<ulong>(uint64ValueCount);

        BooleanValues = reader.ReadArray<bool>(booleanValueCount);
        SingleValues = reader.ReadArray<float>(singleValueCount);
        DoubleValues = reader.ReadArray<double>(doubleValueCount);
        GuidValues = reader.ReadArray<CigGuid>(guidValueCount);

        StringIdValues = reader.ReadArray<DataCoreStringId>(stringIdValueCount);
        LocaleValues = reader.ReadArray<DataCoreStringId>(localeValueCount);
        EnumValues = reader.ReadArray<DataCoreStringId>(enumValueCount);

        StrongValues = reader.ReadArray<DataCorePointer>(strongValueCount);
        WeakValues = reader.ReadArray<DataCorePointer>(weakValueCount);
        ReferenceValues = reader.ReadArray<DataCoreReference>(referenceValueCount);
        EnumOptions = reader.ReadArray<DataCoreStringId2>(enumOptionCount);

        CachedStrings = ReadStringTable(reader.ReadBytes((int)textLength).AsSpan());
        CachedStrings2 = ReadStringTable(reader.ReadBytes((int)textLength2).AsSpan());
        
        var bytesRead = (int)fs.Position;

        Offsets = ReadOffsets(bytesRead, DataMappings, StructDefinitions, PropertyDefinitions);
        DataSectionOffset = bytesRead;
        DataSection = new byte[fs.Length - bytesRead];
        if (reader.Read(DataSection, 0, DataSection.Length) != DataSection.Length)
            throw new Exception("Failed to read data section");
        
        var records = new Dictionary<CigGuid, DataCoreRecord>();
        foreach (var record in RecordDefinitions)
        {
            records[record.Id] = record;
        }
        RecordMap = records.ToFrozenDictionary();

#if DEBUG
        DebugGlobal.Database = this;
#endif
    }

    public SpanReader GetReader(int offset) => new(DataSection, offset - DataSectionOffset);
    public string GetString(DataCoreStringId id) => CachedStrings[id.Id];
    public string GetString2(DataCoreStringId2 id) => CachedStrings2[id.Id];
    public DataCoreRecord GetRecord(CigGuid guid) => RecordMap[guid];

    private static FrozenDictionary<int, string> ReadStringTable(ReadOnlySpan<byte> span)
    {
        var strings = new Dictionary<int, string>();
        var offset = 0;

        while (offset < span.Length)
        {
            var length = span[offset..].IndexOf((byte)0);
            var useful = span[offset..(offset + length)];
            var str = Encoding.ASCII.GetString(useful);
            strings[offset] = str;
            offset += length + 1;
        }

        return strings.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, int[]> ReadOffsets(
        int initialOffset,
        Span<DataCoreDataMapping> mappings,
        ReadOnlySpan<DataCoreStructDefinition> structDefs,
        ReadOnlySpan<DataCorePropertyDefinition> propDefs)
    {
        var instances = new Dictionary<int, int[]>();

        foreach (var mapping in mappings)
        {
            var arr = new int[mapping.StructCount];
            var structDef = structDefs[mapping.StructIndex];
            var structSize = structDef.CalculateSize(structDefs, propDefs);

            for (var i = 0; i < mapping.StructCount; i++)
            {
                arr[i] = initialOffset;
                initialOffset += structSize;
            }

            instances.Add(mapping.StructIndex, arr);
        }

        return instances.ToFrozenDictionary();
    }
}