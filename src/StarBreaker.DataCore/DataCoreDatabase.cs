using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public class DataCoreDatabase
{
    private readonly ConcurrentDictionary<int, DataCorePropertyDefinition[]> _propertiesCache = new();
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
    public readonly FrozenSet<CigGuid> MainRecords;

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

        Offsets = ReadOffsets(bytesRead, DataMappings);
        DataSectionOffset = bytesRead;
        DataSection = new byte[fs.Length - bytesRead];
        if (reader.Read(DataSection, 0, DataSection.Length) != DataSection.Length)
            throw new Exception("Failed to read data section");

        RecordMap = RecordDefinitions.ToFrozenDictionary(x => x.Id);

        var mainRecords = new Dictionary<string, DataCoreRecord>();
        foreach (var record in RecordDefinitions)
        {
            mainRecords[record.GetFileName(this)] = record;
        }

        MainRecords = mainRecords.Values.Select(x => x.Id).ToFrozenSet();

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

    private FrozenDictionary<int, int[]> ReadOffsets(int initialOffset, Span<DataCoreDataMapping> mappings)
    {
        var instances = new Dictionary<int, int[]>(mappings.Length);

        foreach (var mapping in mappings)
        {
            var arr = new int[mapping.StructCount];

            for (var i = 0; i < mapping.StructCount; i++)
            {
                arr[i] = initialOffset;
                initialOffset += CalculateStructSize(mapping.StructIndex);
            }

            instances.Add(mapping.StructIndex, arr);
        }

        return instances.ToFrozenDictionary();
    }

    public DataCorePropertyDefinition[] GetProperties(int structIndex) =>
        _propertiesCache.GetOrAdd(structIndex, static (index, db) =>
        {
            var @this = db.StructDefinitions[index];
            var structs = db.StructDefinitions.AsSpan();
            var properties = db.PropertyDefinitions.AsSpan();

            if (@this is { AttributeCount: 0, ParentTypeIndex: -1 })
                return [];

            // Calculate total property count to avoid resizing
            int totalPropertyCount = @this.AttributeCount;
            var baseStruct = @this;
            while (baseStruct.ParentTypeIndex != -1)
            {
                baseStruct = structs[baseStruct.ParentTypeIndex];
                totalPropertyCount += baseStruct.AttributeCount;
            }

            // Pre-allocate array with exact size needed
            var result = new DataCorePropertyDefinition[totalPropertyCount];

            // Reset to start struct for actual property copying
            baseStruct = @this;
            var currentPosition = totalPropertyCount;

            // Copy properties in reverse order to avoid InsertRange
            do
            {
                int count = baseStruct.AttributeCount;
                currentPosition -= count;
                properties.Slice(baseStruct.FirstAttributeIndex, count)
                    .CopyTo(result.AsSpan(currentPosition, count));

                if (baseStruct.ParentTypeIndex == -1) break;
                baseStruct = structs[baseStruct.ParentTypeIndex];
            } while (true);

            return result;
        }, this);

    public int CalculateStructSize(int structIndex)
    {
        var size = 0;

        foreach (var attribute in GetProperties(structIndex))
        {
            if (attribute.ConversionType != ConversionType.Attribute)
            {
                //array count + array offset
                size += sizeof(int) * 2;
                continue;
            }

            size += attribute.DataType switch
            {
                DataType.Reference => Unsafe.SizeOf<DataCoreReference>(),
                DataType.WeakPointer => Unsafe.SizeOf<DataCorePointer>(),
                DataType.StrongPointer => Unsafe.SizeOf<DataCorePointer>(),
                DataType.EnumChoice => Unsafe.SizeOf<DataCoreStringId>(),
                DataType.Guid => Unsafe.SizeOf<CigGuid>(),
                DataType.Locale => Unsafe.SizeOf<DataCoreStringId>(),
                DataType.Double => Unsafe.SizeOf<double>(),
                DataType.Single => Unsafe.SizeOf<float>(),
                DataType.String => Unsafe.SizeOf<DataCoreStringId>(),
                DataType.UInt64 => Unsafe.SizeOf<ulong>(),
                DataType.UInt32 => Unsafe.SizeOf<uint>(),
                DataType.UInt16 => Unsafe.SizeOf<ushort>(),
                DataType.Byte => Unsafe.SizeOf<byte>(),
                DataType.Int64 => Unsafe.SizeOf<long>(),
                DataType.Int32 => Unsafe.SizeOf<int>(),
                DataType.Int16 => Unsafe.SizeOf<short>(),
                DataType.SByte => Unsafe.SizeOf<sbyte>(),
                DataType.Boolean => Unsafe.SizeOf<byte>(),
                DataType.Class => CalculateStructSize(attribute.StructIndex),
                _ => throw new InvalidOperationException(nameof(DataType))
            };
        }

        return size;
    }
}