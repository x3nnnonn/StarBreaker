using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinaryXml : IDataCoreBinary<string>
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinaryXml(DataCoreDatabase db)
    {
        Database = db;
    }

    public void SaveToFile(DataCoreRecord record, DataCoreExtractionOptions options, string path)
    {
        using var fileStream = new FileStream(Path.ChangeExtension(path, "xml"), FileMode.Create);
        using var writer = XmlWriter.Create(fileStream, new XmlWriterSettings
        {
            Indent = true
        });

        var context = new Context(record.GetFileName(Database), options, writer);

        writer.WriteStartElement(XmlConvert.EncodeName(record.GetName(Database)));
        context.Writer.WriteAttributeString("__RecordId", record.Id.ToString());

        WriteInstance(record.StructIndex, record.InstanceIndex, context);

        context.Writer.WriteEndElement();

        context.Writer.Flush();
    }

    public string GetFromMainRecord(DataCoreRecord record, DataCoreExtractionOptions options)
    {
        if (!Database.MainRecords.Contains(record.Id))
            throw new InvalidOperationException("Can only extract main records");

        using var ms = new MemoryStream();
        using var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true
        });

        var context = new Context(record.GetFileName(Database), options, writer);

        writer.WriteStartElement(XmlConvert.EncodeName(record.GetName(Database)));
        writer.WriteAttributeString("__RecordId", record.Id.ToString());

        WriteInstance(record.StructIndex, record.InstanceIndex, context);

        writer.WriteEndElement();

        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private void WriteInstance(int structIndex, int instanceIndex, Context context)
    {
        var reader = Database.GetReader(structIndex, instanceIndex);

        context.Writer.WriteAttributeString("__Type", Database.StructDefinitions[structIndex].GetName(Database));
        context.Writer.WriteAttributeString("__Pointer", $"__ptr:{structIndex},{instanceIndex}");

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
                context.Writer.WriteStartElement(propName);

                if (reference.RecordId != CigGuid.Empty && reference.InstanceIndex != -1)
                    WriteFromReference(reference, context);

                context.Writer.WriteEndElement();

                break;
            case DataType.WeakPointer:
                var weakPointer = reader.Read<DataCorePointer>();
                context.Writer.WriteStartElement(propName);

                if (weakPointer.StructIndex != -1 && weakPointer.InstanceIndex != -1)
                    context.Writer.WriteAttributeString("__ptr", $"{weakPointer.StructIndex},{weakPointer.InstanceIndex}");

                context.Writer.WriteEndElement();
                break;
            case DataType.StrongPointer:
                var strongPointer = reader.Read<DataCorePointer>();
                context.Writer.WriteStartElement(propName);

                if (strongPointer.StructIndex != -1 && strongPointer.InstanceIndex != -1)
                    WriteInstance(strongPointer.StructIndex, strongPointer.InstanceIndex, context);

                context.Writer.WriteEndElement();
                break;
            case DataType.Class:
                context.Writer.WriteStartElement(propName);

                WriteStruct(prop.StructIndex, ref reader, context);

                context.Writer.WriteEndElement();
                break;
            case DataType.EnumChoice: context.Writer.WriteElementString(propName, reader.Read<DataCoreStringId>().ToString(Database)); break;
            case DataType.Guid: context.Writer.WriteElementString(propName, reader.Read<CigGuid>().ToString()); break;
            case DataType.Locale: context.Writer.WriteElementString(propName, reader.Read<DataCoreStringId>().ToString(Database)); break;
            case DataType.Double: context.Writer.WriteElementString(propName, reader.ReadDouble().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.Single: context.Writer.WriteElementString(propName, reader.ReadSingle().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.String: context.Writer.WriteElementString(propName, reader.Read<DataCoreStringId>().ToString(Database)); break;
            case DataType.UInt64: context.Writer.WriteElementString(propName, reader.ReadUInt64().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.UInt32: context.Writer.WriteElementString(propName, reader.ReadUInt32().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.UInt16: context.Writer.WriteElementString(propName, reader.ReadUInt16().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.Byte: context.Writer.WriteElementString(propName, reader.ReadByte().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.Int64: context.Writer.WriteElementString(propName, reader.ReadInt64().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.Int32: context.Writer.WriteElementString(propName, reader.ReadInt32().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.Int16: context.Writer.WriteElementString(propName, reader.ReadInt16().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.SByte: context.Writer.WriteElementString(propName, reader.ReadSByte().ToString(CultureInfo.InvariantCulture)); break;
            case DataType.Boolean: context.Writer.WriteElementString(propName, reader.ReadBoolean().ToString(CultureInfo.InvariantCulture)); break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WriteArray(DataCorePropertyDefinition prop, ref SpanReader reader, Context context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var propName = prop.GetName(Database);
        context.Writer.WriteStartElement(propName);
        context.Writer.WriteAttributeString("__Count", count.ToString(CultureInfo.InvariantCulture));
        context.Writer.WriteAttributeString("__Type", Database.StructDefinitions[prop.StructIndex].GetName(Database));

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            switch (prop.DataType)
            {
                case DataType.Reference:
                    var reference = Database.ReferenceValues[i];

                    if (reference.RecordId != CigGuid.Empty && reference.InstanceIndex != -1)
                    {
                        // we know the type of this array element. Write the actual type
                        var record = Database.GetRecord(reference.RecordId);
                        var actualTypeName = Database.StructDefinitions[record.StructIndex].GetName(Database);
                        context.Writer.WriteStartElement(actualTypeName);
                        WriteFromReference(Database.ReferenceValues[i], context);
                    }
                    else
                    {
                        // we don't know the type of this array element. Write the type of the array (which might be a base class)
                        context.Writer.WriteStartElement(Database.StructDefinitions[prop.StructIndex].GetName(Database));
                    }

                    context.Writer.WriteEndElement();

                    break;
                case DataType.WeakPointer:
                    var weakPointer = Database.WeakValues[i];
                    if (weakPointer.StructIndex != -1 && weakPointer.InstanceIndex != -1)
                    {
                        var actualTypeName = Database.StructDefinitions[weakPointer.StructIndex].GetName(Database);
                        context.Writer.WriteStartElement(actualTypeName);
                    }
                    else
                    {
                        // we don't know the type of this array element. Write the type of the array (which might be a base class)
                        context.Writer.WriteStartElement(Database.StructDefinitions[prop.StructIndex].GetName(Database));
                    }

                    context.Writer.WriteAttributeString("__ptr", $"{weakPointer.StructIndex},{weakPointer.InstanceIndex}");

                    context.Writer.WriteEndElement();

                    break;
                case DataType.StrongPointer:
                    var strongPointer = Database.StrongValues[i];
                    if (strongPointer.StructIndex != -1 && strongPointer.InstanceIndex != -1)
                    {
                        var actualTypeName = Database.StructDefinitions[strongPointer.StructIndex].GetName(Database);
                        context.Writer.WriteStartElement(actualTypeName);
                        WriteInstance(strongPointer.StructIndex, strongPointer.InstanceIndex, context);
                    }
                    else
                    {
                        // we don't know the type of this array element. Write the type of the array (which might be a base class)
                        context.Writer.WriteStartElement(Database.StructDefinitions[prop.StructIndex].GetName(Database));
                    }

                    context.Writer.WriteEndElement();

                    break;
                case DataType.Class:
                    context.Writer.WriteStartElement(Database.StructDefinitions[prop.StructIndex].GetName(Database));
                    WriteInstance(prop.StructIndex, i, context);
                    context.Writer.WriteEndElement();
                    break;
                case DataType.EnumChoice: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.EnumValues[i].ToString(Database)); break;
                case DataType.Guid: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.GuidValues[i].ToString()); break;
                case DataType.Locale: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.LocaleValues[i].ToString(Database)); break;
                case DataType.Double: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.DoubleValues[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.Single: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.SingleValues[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.String: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.StringIdValues[i].ToString(Database)); break;
                case DataType.UInt64: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.UInt64Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.UInt32: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.UInt32Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.UInt16: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.UInt16Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.Byte: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.UInt8Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.Int64: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.Int64Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.Int32: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.Int32Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.Int16: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.Int16Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.SByte: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.Int8Values[i].ToString(CultureInfo.InvariantCulture)); break;
                case DataType.Boolean: context.Writer.WriteElementString(prop.DataType.ToStringFast(), Database.BooleanValues[i].ToString(CultureInfo.InvariantCulture)); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        context.Writer.WriteEndElement();
    }

    private void WriteFromReference(DataCoreReference reference, Context context)
    {
        var record = Database.GetRecord(reference.RecordId);

        if (Database.MainRecords.Contains(reference.RecordId))
        {
            //if we're referencing a full on file, just add a small mention to it
            context.Writer.WriteElementString("RecordId", record.Id.ToString());
            context.Writer.WriteElementString("Type", Database.StructDefinitions[record.StructIndex].GetName(Database));
            context.Writer.WriteElementString("File", DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.Path));
            return;
        }

        WriteInstance(record.StructIndex, record.InstanceIndex, context);
    }

    private sealed class Context
    {
        public string Path { get; }
        public DataCoreExtractionOptions Options { get; }

        public XmlWriter Writer { get; }

        public Context(string path, DataCoreExtractionOptions options, XmlWriter writer = null)
        {
            Path = path;
            Options = options;
            Writer = writer;
        }
    }
}