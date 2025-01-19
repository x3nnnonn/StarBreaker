using System.Globalization;
using System.Text;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinary
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinary(Stream fs)
    {
        Database = new DataCoreDatabase(fs);
    }

    private XElement GetFromStruct(int structIndex, ref SpanReader reader, DataCoreExtractionContext context)
    {
        var node = new XElement(Database.StructDefinitions[structIndex].GetName(Database));

        foreach (var prop in Database.GetProperties(structIndex))
        {
            node.Add(prop.ConversionType switch
            {
                //TODO: do we need to handle different types of arrays?
                ConversionType.Attribute => GetAttribute(prop, ref reader, context),
                ConversionType.ComplexArray => GetArray(prop, ref reader, context)?.WithAttribute("__type", "ComplexArray", context.Options.ShouldWriteMetadata),
                ConversionType.SimpleArray => GetArray(prop, ref reader, context)?.WithAttribute("__type", "SimpleArray", context.Options.ShouldWriteMetadata),
                ConversionType.ClassArray => GetArray(prop, ref reader, context)?.WithAttribute("__type", "ClassArray", context.Options.ShouldWriteMetadata),
                _ => throw new InvalidOperationException(nameof(ConversionType))
            });
        }

        return node;
    }

    private XElement? GetArray(DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        if (count == 0)
            return null;

        var arrayNode = new XElement(prop.GetName(Database));

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            arrayNode.Add(prop.DataType switch
            {
                DataType.Reference => GetFromReference(Database.ReferenceValues[i], context)?.WithAttribute("__type", "ArrReference", context.Options.ShouldWriteMetadata),
                DataType.WeakPointer => GetWeakPointer(Database.WeakValues[i], context)?.WithAttribute("__type", "ArrWeak", context.Options.ShouldWriteMetadata),
                DataType.StrongPointer => GetFromPointer(Database.StrongValues[i], context)?.WithAttribute("__type", "ArrStrong", context.Options.ShouldWriteMetadata),
                DataType.Class => GetFromInstance(prop.StructIndex, i, context)?.WithAttribute("__type", "ArrClass", context.Options.ShouldWriteMetadata),

                DataType.EnumChoice => new XElement(prop.DataType.ToStringFast(), Database.EnumValues[i].ToString(Database)),
                DataType.Guid => new XElement(prop.DataType.ToStringFast(), Database.GuidValues[i].ToString()),
                DataType.Locale => new XElement(prop.DataType.ToStringFast(), Database.LocaleValues[i].ToString(Database)),
                DataType.Double => new XElement(prop.DataType.ToStringFast(), Database.DoubleValues[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Single => new XElement(prop.DataType.ToStringFast(), Database.SingleValues[i].ToString(CultureInfo.InvariantCulture)),
                DataType.String => new XElement(prop.DataType.ToStringFast(), Database.StringIdValues[i].ToString(Database)),
                DataType.UInt64 => new XElement(prop.DataType.ToStringFast(), Database.UInt64Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.UInt32 => new XElement(prop.DataType.ToStringFast(), Database.UInt32Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.UInt16 => new XElement(prop.DataType.ToStringFast(), Database.UInt16Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Byte => new XElement(prop.DataType.ToStringFast(), Database.UInt8Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Int64 => new XElement(prop.DataType.ToStringFast(), Database.Int64Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Int32 => new XElement(prop.DataType.ToStringFast(), Database.Int32Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Int16 => new XElement(prop.DataType.ToStringFast(), Database.Int16Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.SByte => new XElement(prop.DataType.ToStringFast(), Database.Int8Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Boolean => new XElement(prop.DataType.ToStringFast(), Database.BooleanValues[i].ToString(CultureInfo.InvariantCulture)),
                _ => throw new InvalidOperationException(nameof(DataType))
            });
        }

        return arrayNode;
    }

    private XObject? GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext context)
    {
        return prop.DataType switch
        {
            DataType.Reference => GetFromReference(reader.Read<DataCoreReference>(), context)?.WithAttribute("__type", "AttReference", context.Options.ShouldWriteMetadata),
            DataType.WeakPointer => GetWeakPointer(reader.Read<DataCorePointer>(), context)?.WithAttribute("__type", "AttWeak", context.Options.ShouldWriteMetadata),
            DataType.StrongPointer => GetFromPointer(reader.Read<DataCorePointer>(), context)?.WithAttribute("__type", "AttStrong", context.Options.ShouldWriteMetadata),
            DataType.Class => GetFromStruct(prop.StructIndex, ref reader, context)?.WithAttribute("__type", "AttClass", context.Options.ShouldWriteMetadata),

            DataType.EnumChoice => new XAttribute(prop.GetName(Database), reader.Read<DataCoreStringId>().ToString(Database)),
            DataType.Guid => new XAttribute(prop.GetName(Database), reader.Read<CigGuid>().ToString()),
            DataType.Locale => new XAttribute(prop.GetName(Database), reader.Read<DataCoreStringId>().ToString(Database)),
            DataType.Double => new XAttribute(prop.GetName(Database), reader.ReadDouble().ToString(CultureInfo.InvariantCulture)),
            DataType.Single => new XAttribute(prop.GetName(Database), reader.ReadSingle().ToString(CultureInfo.InvariantCulture)),
            DataType.String => new XAttribute(prop.GetName(Database), reader.Read<DataCoreStringId>().ToString(Database)),
            DataType.UInt64 => new XAttribute(prop.GetName(Database), reader.ReadUInt64().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt32 => new XAttribute(prop.GetName(Database), reader.ReadUInt32().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt16 => new XAttribute(prop.GetName(Database), reader.ReadUInt16().ToString(CultureInfo.InvariantCulture)),
            DataType.Byte => new XAttribute(prop.GetName(Database), reader.ReadByte().ToString(CultureInfo.InvariantCulture)),
            DataType.Int64 => new XAttribute(prop.GetName(Database), reader.ReadInt64().ToString(CultureInfo.InvariantCulture)),
            DataType.Int32 => new XAttribute(prop.GetName(Database), reader.ReadInt32().ToString(CultureInfo.InvariantCulture)),
            DataType.Int16 => new XAttribute(prop.GetName(Database), reader.ReadInt16().ToString(CultureInfo.InvariantCulture)),
            DataType.SByte => new XAttribute(prop.GetName(Database), reader.ReadSByte().ToString(CultureInfo.InvariantCulture)),
            DataType.Boolean => new XAttribute(prop.GetName(Database), reader.ReadBoolean().ToString(CultureInfo.InvariantCulture)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private XElement? GetFromReference(DataCoreReference reference, DataCoreExtractionContext context)
    {
        if (reference.InstanceIndex == -1 || reference.RecordId == CigGuid.Empty)
        {
            if (!context.Options.ShouldWriteNulls)
                return null;

            var invalidNode = new XElement("NullReference");
            invalidNode.Add(new XAttribute("guid", reference.RecordId.ToString()));
            if (context.Options.ShouldWriteMetadata)
            {
                invalidNode.Add(new XAttribute("__instanceIndex", reference.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
            }

            return invalidNode;
        }

        var record = Database.GetRecord(reference.RecordId);

        if (Database.MainRecords.Contains(reference.RecordId))
        {
            //if we're referencing a full on file, just add a small mention to it
            var fileReferenceNode = new XElement("FileReference");
            fileReferenceNode.Add(new XAttribute("guid", reference.RecordId.ToString()));
            fileReferenceNode.Add(new XAttribute("filePath", DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.FileName)));
            return fileReferenceNode;
        }

        return GetFromRecord(record, context);
    }

    private XElement GetFromRecord(DataCoreRecord record, DataCoreExtractionContext context)
    {
        return GetFromInstance(record.StructIndex, record.InstanceIndex, context)
            .WithAttribute("recordGuid", record.Id.ToString());
            //Note: Maybe we should add the name? It seems to be mostly useless. Usually it's
            //      just a combination of the record type and the filename | recordId.
            //.WithAttribute("recordName", record.GetName(Database));
    }

    public XElement GetFromMainRecord(DataCoreRecord record, DataCoreExtractionContext context)
    {
        if (!Database.MainRecords.Contains(record.Id))
            throw new InvalidOperationException("Can only extract main records");

        var element = GetFromRecord(record, context);

        //add weak pointers ids, so we can actually see what a weak pointer is pointing at
        foreach (var weakPtr in context.GetWeakPointers())
        {
            var pointedAtElement = context.Elements[(weakPtr.structIndex, weakPtr.instanceIndex)];

            pointedAtElement.Add(new XAttribute("weakPointerId", context.GetWeakPointerId(weakPtr.structIndex, weakPtr.instanceIndex).ToString(CultureInfo.InvariantCulture)));
        }

        return element;
    }

    private XElement? GetFromPointer(DataCorePointer pointer, DataCoreExtractionContext context)
    {
        if (pointer.InstanceIndex == -1 || pointer.StructIndex == -1)
            return GetNullPointer(pointer, context);

        return GetFromInstance(pointer.StructIndex, pointer.InstanceIndex, context);
    }

    private XElement GetFromInstance(int structIndex, int instanceIndex, DataCoreExtractionContext context)
    {
        var reader = Database.GetReader(structIndex, instanceIndex);
        var element = GetFromStruct(structIndex, ref reader, context);

        context.Elements[(structIndex, instanceIndex)] = element;

        if (context.Options.ShouldWriteMetadata)
        {
            element.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
            element.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));
        }

        return element;
    }

    private XElement? GetWeakPointer(DataCorePointer pointer, DataCoreExtractionContext context)
    {
        if (pointer.InstanceIndex == -1 || pointer.StructIndex == -1)
            return GetNullPointer(pointer, context);

        var pointerId = context.AddWeakPointer(pointer.StructIndex, pointer.InstanceIndex);

        var invalidNode = new XElement("WeakPointer");

        invalidNode.Add(new XAttribute("weakPointerId", pointerId.ToString(CultureInfo.InvariantCulture)));
        var structName = Database.StructDefinitions[pointer.StructIndex].GetName(Database);
        invalidNode.Add(new XAttribute("structName", structName));

        if (context.Options.ShouldWriteMetadata)
        {
            invalidNode.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
            invalidNode.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
        }

        return invalidNode;
    }

    private static XElement? GetNullPointer(DataCorePointer pointer, DataCoreExtractionContext context)
    {
        if (!context.Options.ShouldWriteNulls)
            return null;

        var invalidNode = new XElement("NullPointer");
        if (context.Options.ShouldWriteMetadata)
        {
            invalidNode.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
            invalidNode.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
        }

        return invalidNode;
    }
}