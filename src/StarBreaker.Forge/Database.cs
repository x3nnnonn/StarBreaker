using System.Collections.Frozen;
using System.Text;
using StarBreaker.Common;

namespace StarBreaker.Forge;

public class Database
{
    private readonly int DataSectionOffset;
    private readonly byte[] DataSection;
    
    public readonly DataForgeStructDefinition[] StructDefinitions;
    public readonly DataForgePropertyDefinition[] PropertyDefinitions;
    public readonly DataForgeEnumDefinition[] EnumDefinitions;
    public readonly DataForgeDataMapping[] DataMappings;
    public readonly DataForgeRecord[] RecordDefinitions;

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

    public readonly DataForgeStringId[] StringIdValues;
    public readonly DataForgeStringId[] LocaleValues;
    public readonly DataForgeStringId[] EnumValues;

    public readonly DataForgePointer[] StrongValues;
    public readonly DataForgePointer[] WeakValues;
    public readonly DataForgeReference[] ReferenceValues;

    public readonly DataForgeStringId[] EnumOptions;

    private readonly FrozenDictionary<int, string> _cachedStrings;
    private readonly FrozenDictionary<int, string> _cachedStrings2;

    public Database(ReadOnlySpan<byte> bytes, out int bytesRead)
    {
        var reader = new SpanReader(bytes, 0);
        _ = reader.ReadUInt32();
        var fileVersion = reader.ReadUInt32();
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

        StructDefinitions = reader.ReadSpan<DataForgeStructDefinition>(structDefinitionCount).ToArray();
        PropertyDefinitions = reader.ReadSpan<DataForgePropertyDefinition>(propertyDefinitionCount).ToArray();
        EnumDefinitions = reader.ReadSpan<DataForgeEnumDefinition>(enumDefinitionCount).ToArray();
        DataMappings = reader.ReadSpan<DataForgeDataMapping>(dataMappingCount).ToArray();
        RecordDefinitions = reader.ReadSpan<DataForgeRecord>(recordDefinitionCount).ToArray();

        Int8Values = reader.ReadSpan<sbyte>(int8ValueCount).ToArray();
        Int16Values = reader.ReadSpan<short>(int16ValueCount).ToArray();
        Int32Values = reader.ReadSpan<int>(int32ValueCount).ToArray();
        Int64Values = reader.ReadSpan<long>(int64ValueCount).ToArray();

        UInt8Values = reader.ReadSpan<byte>(uint8ValueCount).ToArray();
        UInt16Values = reader.ReadSpan<ushort>(uint16ValueCount).ToArray();
        UInt32Values = reader.ReadSpan<uint>(uint32ValueCount).ToArray();
        UInt64Values = reader.ReadSpan<ulong>(uint64ValueCount).ToArray();

        BooleanValues = reader.ReadSpan<bool>(booleanValueCount).ToArray();
        SingleValues = reader.ReadSpan<float>(singleValueCount).ToArray();
        DoubleValues = reader.ReadSpan<double>(doubleValueCount).ToArray();
        GuidValues = reader.ReadSpan<CigGuid>(guidValueCount).ToArray();

        StringIdValues = reader.ReadSpan<DataForgeStringId>(stringIdValueCount).ToArray();
        LocaleValues = reader.ReadSpan<DataForgeStringId>(localeValueCount).ToArray();
        EnumValues = reader.ReadSpan<DataForgeStringId>(enumValueCount).ToArray();

        StrongValues = reader.ReadSpan<DataForgePointer>(strongValueCount).ToArray();
        WeakValues = reader.ReadSpan<DataForgePointer>(weakValueCount).ToArray();
        ReferenceValues = reader.ReadSpan<DataForgeReference>(referenceValueCount).ToArray();
        EnumOptions = reader.ReadSpan<DataForgeStringId>(enumOptionCount).ToArray();
        
        var stringSpan = reader.ReadBytes((int)textLength);

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

        _cachedStrings = strings.ToFrozenDictionary();
        
        bytesRead = reader.Position;

        DataSectionOffset = bytesRead;
        DataSection = new byte[bytes.Length - bytesRead];
        bytes[bytesRead..].CopyTo(DataSection);
#if DEBUG
        DebugGlobal.Database = this;
#endif
    }
    
    public Database(string filePath, out int bytesRead)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs);
        
        _ = reader.ReadUInt32();
        var fileVersion = reader.ReadUInt32();
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

        StructDefinitions = reader.ReadArray<DataForgeStructDefinition>(structDefinitionCount);
        PropertyDefinitions = reader.ReadArray<DataForgePropertyDefinition>(propertyDefinitionCount);
        EnumDefinitions = reader.ReadArray<DataForgeEnumDefinition>(enumDefinitionCount);
        DataMappings = reader.ReadArray<DataForgeDataMapping>(dataMappingCount);
        RecordDefinitions = reader.ReadArray<DataForgeRecord>(recordDefinitionCount);

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

        StringIdValues = reader.ReadArray<DataForgeStringId>(stringIdValueCount);
        LocaleValues = reader.ReadArray<DataForgeStringId>(localeValueCount);
        EnumValues = reader.ReadArray<DataForgeStringId>(enumValueCount);

        StrongValues = reader.ReadArray<DataForgePointer>(strongValueCount);
        WeakValues = reader.ReadArray<DataForgePointer>(weakValueCount);
        ReferenceValues = reader.ReadArray<DataForgeReference>(referenceValueCount);
        EnumOptions = reader.ReadArray<DataForgeStringId>(enumOptionCount);

        var stringSpan = reader.ReadBytes((int)textLength).AsSpan();
        var stringSpan2 = reader.ReadBytes((int)textLength2).AsSpan();

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
        
        var strings2 = new Dictionary<int, string>();
        var offset2 = 0;
        while (offset2 < stringSpan2.Length)
        {
            var length = stringSpan2[offset2..].IndexOf((byte)0);
            var useful = stringSpan2[offset2..(offset2 + length)];
            var str = Encoding.ASCII.GetString(useful);
            strings2[offset2] = str;
            offset2 += length + 1;
        }

        _cachedStrings = strings.ToFrozenDictionary();
        _cachedStrings2 = strings2.ToFrozenDictionary();
        
        bytesRead = (int)fs.Position;

        DataSectionOffset = bytesRead;
        DataSection = new byte[fs.Length - bytesRead];
        if (reader.Read(DataSection, 0, DataSection.Length) != DataSection.Length)
            throw new Exception("Failed to read data section");
        
#if DEBUG
        DebugGlobal.Database = this;
#endif
    }
    
    public SpanReader GetReader(int offset) => new(DataSection, offset - DataSectionOffset);
    public string GetString(DataForgeStringId id) => _cachedStrings[id.Id];
    public string GetString2(DataForgeStringId2 id) => _cachedStrings2[id.Id];
}