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
                ConversionType.ComplexArray => GetArray(prop, ref reader, context),
                ConversionType.SimpleArray => GetArray(prop, ref reader, context),
                ConversionType.ClassArray => GetArray(prop, ref reader, context),
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
                DataType.Reference => GetFromReference(Database.ReferenceValues[i], context),
                DataType.WeakPointer => GetFromPointer(Database.WeakValues[i], context),
                DataType.StrongPointer => GetFromPointer(Database.StrongValues[i], context),
                DataType.Class => GetFromInstance(prop.StructIndex, i, context),

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

    private XObject GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext context)
    {
        return prop.DataType switch
        {
            DataType.Reference => GetFromReference(reader.Read<DataCoreReference>(), context),
            DataType.WeakPointer => GetFromPointer(reader.Read<DataCorePointer>(), context),
            DataType.StrongPointer => GetFromPointer(reader.Read<DataCorePointer>(), context),
            DataType.Class => GetFromStruct(prop.StructIndex, ref reader, context),

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

    private XElement GetFromReference(DataCoreReference reference, DataCoreExtractionContext context)
    {
        if (reference.IsInvalid)
        {
            var invalidNode = new XElement("NullReference");
            invalidNode.Add(new XAttribute("__guid", reference.RecordId.ToString()));
            invalidNode.Add(new XAttribute("__instanceIndex", reference.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
            return invalidNode;
        }

        var record = Database.GetRecord(reference.RecordId);

        if (Database.MainRecords.Contains(reference.RecordId))
        {
            //if we're referencing a full on file, just add a small mention to it
            var fileReferenceNode = new XElement("FileReference");
            fileReferenceNode.Add(new XAttribute("__guid", reference.RecordId.ToString()));
            fileReferenceNode.Add(new XAttribute("__filePath", ComputeRelativePath(record.GetFileName(Database), context.FileName)));
            return fileReferenceNode;
        }

        return GetFromRecord(record, context);
    }

    public XElement GetFromRecord(DataCoreRecord record, DataCoreExtractionContext context)
    {
        var element = GetFromInstance(record.StructIndex, record.InstanceIndex, context);
        element.Add(new XAttribute("__recordGuid", record.Id.ToString()));
        return element;
    }

    private XElement GetFromPointer(DataCorePointer pointer, DataCoreExtractionContext context)
    {
        if (pointer.IsInvalid)
        {
            var invalidNode = new XElement("NullPointer");
            invalidNode.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
            invalidNode.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
            return invalidNode;
        }

        return GetFromInstance(pointer.StructIndex, pointer.InstanceIndex, context);
    }

    private XElement GetFromInstance(int structIndex, int instanceIndex, DataCoreExtractionContext context)
    {
        //if (context.Tracker.Contains((structIndex, instanceIndex)))
        if (!context.Tracker.Add((structIndex, instanceIndex)))
        {
            var circularNode = new XElement("CircularReference");

            circularNode.Add(new XAttribute("__structName", Database.StructDefinitions[structIndex].GetName(Database)));
            circularNode.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
            circularNode.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));

            return circularNode;
        }

        //context.Tracker.Push((structIndex, instanceIndex));

        var reader = Database.GetReader(Database.Offsets[structIndex][instanceIndex]);
        var element = GetFromStruct(structIndex, ref reader, context);

        //context.Tracker.Pop();

        // add some metadata to the element, mostly so we can figure out what a CircularReference is pointing to
        element.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
        element.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));

        return element;
    }

    public static string ComputeRelativePath(string filePath, string contextFileName)
    {
        var slashes = contextFileName.Count(c => c == '/');
        var sb = new StringBuilder("file://./");
        for (var i = 0; i < slashes; i++)
        {
            sb.Append("../");
        }
        sb.Append(filePath);
        return sb.ToString();
    }
}