using System.Text.Json;
using System.Text.Json.Nodes;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinaryJsonObject : IDataCoreBinary<JsonObject>
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinaryJsonObject(DataCoreDatabase db)
    {
        Database = db;
    }

    public void SaveToFile(DataCoreRecord record, string path)
    {
        using var stream = File.OpenWrite(Path.ChangeExtension(path, "json"));
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true
        });
        var jsonRecord = GetFromMainRecord(record);
        jsonRecord.WriteTo(writer);
    }

    public JsonObject GetFromMainRecord(DataCoreRecord record)
    {
        if (!Database.MainRecords.Contains(record.Id))
            throw new InvalidOperationException("Can only extract main records");

        var context = new Context(record.GetFileName(Database), DataCoreBinaryWalker.WalkRecord(record, Database));

        return new JsonObject
        {
            { "_RecordName_", record.GetName(Database) },
            { "_RecordId_", record.Id.ToString() },
            { "_RecordValue_", GetInstance(record.StructIndex, record.InstanceIndex, context) }
        };
    }

    private JsonObject GetInstance(int structIndex, int instanceIndex, Context context)
    {
        var reader = Database.GetReader(structIndex, instanceIndex);
        var jsonObject = GetStruct(structIndex, ref reader, context);

        if (context.Pointers.TryGetValue((structIndex, instanceIndex), out var pointerIndex))
            jsonObject.Insert(0, "_PointerId_", JsonValue.Create($"ptr:{pointerIndex}"));

        return jsonObject;
    }

    private JsonObject GetStruct(int structIndex, ref SpanReader reader, Context context)
    {
        var obj = new JsonObject();

        obj.Add("_Type_", Database.StructDefinitions[structIndex].GetName(Database));

        foreach (var prop in Database.GetProperties(structIndex))
        {
            obj.Add(prop.GetName(Database), prop.ConversionType switch
            {
                ConversionType.Attribute => GetNode(prop, ref reader, context),
                _ => GetArray(prop, ref reader, context)
            });
        }

        return obj;
    }

    private JsonNode? GetNode(DataCorePropertyDefinition prop, ref SpanReader reader, Context context) => prop.DataType switch
    {
        DataType.Class => GetStruct(prop.StructIndex, ref reader, context),
        DataType.WeakPointer => GetFromWeakPointer(reader.Read<DataCorePointer>(), context),
        DataType.StrongPointer => GetFromStrongPointer(reader.Read<DataCorePointer>(), context),
        DataType.Reference => GetFromReference(reader.Read<DataCoreReference>(), context),
        DataType.EnumChoice => JsonValue.Create(reader.Read<DataCoreStringId>().ToString(Database)),
        DataType.Guid => JsonValue.Create(reader.Read<CigGuid>().ToString()),
        DataType.Locale => JsonValue.Create(reader.Read<DataCoreStringId>().ToString(Database)),
        DataType.Double => JsonValue.Create(reader.ReadDouble()),
        DataType.Single => JsonValue.Create(reader.ReadSingle()),
        DataType.String => JsonValue.Create(reader.Read<DataCoreStringId>().ToString(Database)),
        DataType.UInt64 => JsonValue.Create(reader.ReadUInt64()),
        DataType.UInt32 => JsonValue.Create(reader.ReadUInt32()),
        DataType.UInt16 => JsonValue.Create(reader.ReadUInt16()),
        DataType.Byte => JsonValue.Create(reader.ReadByte()),
        DataType.Int64 => JsonValue.Create(reader.ReadInt64()),
        DataType.Int32 => JsonValue.Create(reader.ReadInt32()),
        DataType.Int16 => JsonValue.Create(reader.ReadInt16()),
        DataType.SByte => JsonValue.Create(reader.ReadSByte()),
        DataType.Boolean => JsonValue.Create(reader.ReadBoolean()),
        _ => throw new ArgumentOutOfRangeException(nameof(prop))
    };

    private JsonNode? GetArrayNode(DataCorePropertyDefinition prop, int instanceIndex, Context context) => prop.DataType switch
    {
        DataType.Class => GetInstance(prop.StructIndex, instanceIndex, context),
        DataType.WeakPointer => GetFromWeakPointer(Database.WeakValues[instanceIndex], context),
        DataType.StrongPointer => GetFromStrongPointer(Database.StrongValues[instanceIndex], context),
        DataType.Reference => GetFromReference(Database.ReferenceValues[instanceIndex], context),
        DataType.EnumChoice => JsonValue.Create(Database.EnumValues[instanceIndex].ToString(Database)),
        DataType.Guid => JsonValue.Create(Database.GuidValues[instanceIndex].ToString()),
        DataType.Locale => JsonValue.Create(Database.LocaleValues[instanceIndex].ToString(Database)),
        DataType.Double => JsonValue.Create(Database.DoubleValues[instanceIndex]),
        DataType.Single => JsonValue.Create(Database.SingleValues[instanceIndex]),
        DataType.String => JsonValue.Create(Database.StringIdValues[instanceIndex].ToString(Database)),
        DataType.UInt64 => JsonValue.Create(Database.UInt64Values[instanceIndex]),
        DataType.UInt32 => JsonValue.Create(Database.UInt32Values[instanceIndex]),
        DataType.UInt16 => JsonValue.Create(Database.UInt16Values[instanceIndex]),
        DataType.Byte => JsonValue.Create(Database.Int8Values[instanceIndex]),
        DataType.Int64 => JsonValue.Create(Database.Int64Values[instanceIndex]),
        DataType.Int32 => JsonValue.Create(Database.Int32Values[instanceIndex]),
        DataType.Int16 => JsonValue.Create(Database.Int16Values[instanceIndex]),
        DataType.SByte => JsonValue.Create(Database.Int8Values[instanceIndex]),
        DataType.Boolean => JsonValue.Create(Database.BooleanValues[instanceIndex]),
        _ => throw new ArgumentOutOfRangeException(nameof(prop))
    };

    private JsonArray GetArray(DataCorePropertyDefinition prop, ref SpanReader reader, Context context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new JsonArray();

        for (var i = firstIndex; i < firstIndex + count; i++)
            array.Add(GetArrayNode(prop, i, context));

        return array;
    }

    private JsonNode? GetFromReference(DataCoreReference reference, Context context)
    {
        if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == -1)
            return null;

        var record = Database.GetRecord(reference.RecordId);

        //if we're referencing a full on file, just add a small mention to it
        if (Database.MainRecords.Contains(reference.RecordId))
            return JsonValue.Create(Path.ChangeExtension(DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.RecordFilePath), "json"));

        //if we're referencing a record in the same file, write it out
        if (record.GetFileName(Database) == context.RecordFilePath)
        {
            var recordNode = GetInstance(record.StructIndex, record.InstanceIndex, context);

            recordNode.Insert(0, "_RecordName_", JsonValue.Create(record.GetName(Database)));
            recordNode.Insert(0, "_RecordId_", JsonValue.Create(reference.RecordId.ToString()));

            return recordNode;
        }

        //if we're referencing a record that's part of another file, mention it
        return new JsonObject
        {
            { "_RecordPath_", Path.ChangeExtension(DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.RecordFilePath), "json") },
            { "_RecordName_", record.GetName(Database) },
            { "_RecordId_", reference.RecordId.ToString() }
        };
    }

    private JsonObject? GetFromStrongPointer(DataCorePointer strongPointer, Context context)
    {
        if (strongPointer.StructIndex == -1 || strongPointer.InstanceIndex == -1)
            return null;

        return GetInstance(strongPointer.StructIndex, strongPointer.InstanceIndex, context);
    }

    private static JsonValue? GetFromWeakPointer(DataCorePointer weakPointer, Context context)
    {
        if (weakPointer.StructIndex == -1 || weakPointer.InstanceIndex == -1)
            return null;

        return JsonValue.Create($"_PointsTo_:ptr:{context.Pointers[(weakPointer.StructIndex, weakPointer.InstanceIndex)]}");
    }

    private readonly struct Context
    {
        public string RecordFilePath { get; }
        public Dictionary<(int, int), int> Pointers { get; }

        public Context(string recordFilePath, Dictionary<(int, int), int> pointers)
        {
            RecordFilePath = recordFilePath;
            Pointers = pointers;
        }
    }
}