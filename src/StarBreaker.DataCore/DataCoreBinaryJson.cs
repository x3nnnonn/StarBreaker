using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinaryJson : IDataCoreBinary<string>
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinaryJson(DataCoreDatabase db)
    {
        Database = db;
    }

    public void SaveToFile(DataCoreRecord record, DataCoreExtractionOptions options, string path)
    {
        using var fileStream = new FileStream(Path.ChangeExtension(path, "json"), FileMode.Create);
        using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions
        {
            Indented = true,
        });

        var context = new Context(record.GetFileName(Database), writer, options);

        context.Writer.WriteStartObject();
        context.Writer.WriteString("__Name", record.GetName(Database));
        context.Writer.WriteString("__RecordId", record.Id.ToString());

        //context.Writer.WriteStartObject("__Record");
        WriteInstance(record.StructIndex, record.InstanceIndex, context);
        //context.Writer.WriteEndObject();

        context.Writer.WriteEndObject();

        context.Writer.Flush();
    }

    public string GetFromMainRecord(DataCoreRecord record, DataCoreExtractionOptions options)
    {
        if (!Database.MainRecords.Contains(record.Id))
            throw new InvalidOperationException("Can only extract main records");

        using var stringWriter = new MemoryStream();
        using var writer = new Utf8JsonWriter(stringWriter, new JsonWriterOptions
        {
            Indented = false
        });

        var context = new Context(record.GetFileName(Database), writer, options);

        writer.WriteStartObject();
        writer.WriteString("__Name", record.GetName(Database));
        writer.WriteString("__RecordId", record.Id.ToString());

        WriteInstance(record.StructIndex, record.InstanceIndex, context);

        writer.WriteEndObject();

        writer.Flush();

        return Encoding.UTF8.GetString(stringWriter.ToArray());
    }

    private void WriteInstance(int structIndex, int instanceIndex, Context context)
    {
        var reader = Database.GetReader(structIndex, instanceIndex);

        context.Writer.WriteString("__Type", Database.StructDefinitions[structIndex].GetName(Database));
        context.Writer.WriteString("__Pointer", $"__ptr:{structIndex},{instanceIndex}");

        WriteStruct(structIndex, ref reader, context);
    }

    private void WriteStruct(int structIndex, ref SpanReader reader, Context context)
    {
        foreach (var prop in Database.GetProperties(structIndex))
        {
            if (prop.ConversionType == ConversionType.Attribute)
                WriteAttribute(prop, ref reader, context);
            else
                WriteArray(prop, ref reader, context);
        }
    }

    private void WriteAttribute(DataCorePropertyDefinition prop, ref SpanReader reader, Context context)
    {
        var propName = prop.GetName(Database);

        switch (prop.DataType)
        {
            case DataType.Reference:
                var reference = reader.Read<DataCoreReference>();
                if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == -1)
                {
                    context.Writer.WriteNull(propName);
                }
                else
                {
                    context.Writer.WriteStartObject(propName);
                    WriteFromReference(reference, context);
                    context.Writer.WriteEndObject();
                }

                break;
            case DataType.WeakPointer:
                var weakPointer = reader.Read<DataCorePointer>();
                if (weakPointer.StructIndex == -1 || weakPointer.InstanceIndex == -1)
                    context.Writer.WriteNull(propName);
                else
                    context.Writer.WriteString(propName, $"__ptr:{weakPointer.StructIndex},{weakPointer.InstanceIndex}");

                break;
            case DataType.StrongPointer:
                var strongPointer = reader.Read<DataCorePointer>();
                if (strongPointer.StructIndex == -1 || strongPointer.InstanceIndex == -1)
                {
                    context.Writer.WriteNull(propName);
                }
                else
                {
                    context.Writer.WriteStartObject(propName);
                    WriteFromStrongPointer(strongPointer, context);
                    context.Writer.WriteEndObject();
                }

                break;
            case DataType.Class:
                context.Writer.WriteStartObject(propName);
                WriteStruct(prop.StructIndex, ref reader, context);
                context.Writer.WriteEndObject();
                break;
            case DataType.EnumChoice: context.Writer.WriteString(propName, reader.Read<DataCoreStringId>().ToString(Database)); break;
            case DataType.Guid: context.Writer.WriteString(propName, reader.Read<CigGuid>().ToString()); break;
            case DataType.Locale: context.Writer.WriteString(propName, reader.Read<DataCoreStringId>().ToString(Database)); break;
            case DataType.Double: context.Writer.WriteNumber(propName, reader.ReadDouble()); break;
            case DataType.Single: context.Writer.WriteNumber(propName, reader.ReadSingle()); break;
            case DataType.String: context.Writer.WriteString(propName, reader.Read<DataCoreStringId>().ToString(Database)); break;
            case DataType.UInt64: context.Writer.WriteNumber(propName, reader.ReadUInt64()); break;
            case DataType.UInt32: context.Writer.WriteNumber(propName, reader.ReadUInt32()); break;
            case DataType.UInt16: context.Writer.WriteNumber(propName, reader.ReadUInt16()); break;
            case DataType.Byte: context.Writer.WriteNumber(propName, reader.ReadByte()); break;
            case DataType.Int64: context.Writer.WriteNumber(propName, reader.ReadInt64()); break;
            case DataType.Int32: context.Writer.WriteNumber(propName, reader.ReadInt32()); break;
            case DataType.Int16: context.Writer.WriteNumber(propName, reader.ReadInt16()); break;
            case DataType.SByte: context.Writer.WriteNumber(propName, reader.ReadSByte()); break;
            case DataType.Boolean: context.Writer.WriteBoolean(propName, reader.ReadBoolean()); break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WriteArray(DataCorePropertyDefinition prop, ref SpanReader reader, Context context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var propName = prop.GetName(Database);
        context.Writer.WriteStartArray(propName);

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            switch (prop.DataType)
            {
                case DataType.Reference:
                    var reference = Database.ReferenceValues[i];
                    if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == -1)
                    {
                        context.Writer.WriteNullValue();
                    }
                    else
                    {
                        context.Writer.WriteStartObject();
                        WriteFromReference(Database.ReferenceValues[i], context);
                        context.Writer.WriteEndObject();
                    }

                    break;
                case DataType.WeakPointer:
                    var weakPointer = Database.WeakValues[i];
                    if (weakPointer.StructIndex == -1 || weakPointer.InstanceIndex == -1)
                    {
                        context.Writer.WriteNullValue();
                    }
                    else
                    {
                        context.Writer.WriteStringValue($"__ptr:{weakPointer.StructIndex},{weakPointer.InstanceIndex}");
                    }

                    break;
                case DataType.StrongPointer:
                    var strongPointer = Database.StrongValues[i];
                    if (strongPointer.StructIndex == -1 || strongPointer.InstanceIndex == -1)
                    {
                        context.Writer.WriteNullValue();
                    }
                    else
                    {
                        context.Writer.WriteStartObject();
                        WriteFromStrongPointer(Database.StrongValues[i], context);
                        context.Writer.WriteEndObject();
                    }

                    break;
                case DataType.Class:
                    context.Writer.WriteStartObject();
                    WriteInstance(prop.StructIndex, i, context);
                    context.Writer.WriteEndObject();
                    break;
                case DataType.EnumChoice: context.Writer.WriteStringValue(Database.EnumValues[i].ToString(Database)); break;
                case DataType.Guid: context.Writer.WriteStringValue(Database.GuidValues[i].ToString()); break;
                case DataType.Locale: context.Writer.WriteStringValue(Database.LocaleValues[i].ToString(Database)); break;
                case DataType.Double: context.Writer.WriteNumberValue(Database.DoubleValues[i]); break;
                case DataType.Single: context.Writer.WriteNumberValue(Database.SingleValues[i]); break;
                case DataType.String: context.Writer.WriteStringValue(Database.StringIdValues[i].ToString(Database)); break;
                case DataType.UInt64: context.Writer.WriteNumberValue(Database.UInt64Values[i]); break;
                case DataType.UInt32: context.Writer.WriteNumberValue(Database.UInt32Values[i]); break;
                case DataType.UInt16: context.Writer.WriteNumberValue(Database.UInt16Values[i]); break;
                case DataType.Byte: context.Writer.WriteNumberValue(Database.UInt8Values[i]); break;
                case DataType.Int64: context.Writer.WriteNumberValue(Database.Int64Values[i]); break;
                case DataType.Int32: context.Writer.WriteNumberValue(Database.Int32Values[i]); break;
                case DataType.Int16: context.Writer.WriteNumberValue(Database.Int16Values[i]); break;
                case DataType.SByte: context.Writer.WriteNumberValue(Database.Int8Values[i]); break;
                case DataType.Boolean: context.Writer.WriteBooleanValue(Database.BooleanValues[i]); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        context.Writer.WriteEndArray();
    }

    private void WriteFromStrongPointer(DataCorePointer pointer, Context context) => WriteInstance(pointer.StructIndex, pointer.InstanceIndex, context);

    private void WriteFromReference(DataCoreReference reference, Context context)
    {
        var record = Database.GetRecord(reference.RecordId);

        if (Database.MainRecords.Contains(reference.RecordId))
        {
            //if we're referencing a full on file, just add a small mention to it
            context.Writer.WriteString("RecordId", record.Id.ToString());
            context.Writer.WriteString("Type", Database.StructDefinitions[record.StructIndex].GetName(Database));
            context.Writer.WriteString("File", Path.ChangeExtension(DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.Path), "json"));
            return;
        }

        WriteInstance(record.StructIndex, record.InstanceIndex, context);
    }

    private sealed class Context
    {
        public string Path { get; }
        public Utf8JsonWriter Writer { get; }
        public DataCoreExtractionOptions Options { get; }

        public Context(string path, Utf8JsonWriter writer, DataCoreExtractionOptions options)
        {
            Path = path;
            Writer = writer;
            Options = options;
        }
    }
}