using System.Globalization;
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

    private XElement GetFromStruct(int structIndex, ref SpanReader reader, Stack<(int, int)> tracker)
    {
        var node = new XElement(Database.StructDefinitions[structIndex].GetName(Database));

        foreach (var prop in Database.GetProperties(structIndex))
        {
            node.Add(prop.ConversionType switch
            {
                //TODO: do we need to handle different types of arrays?
                ConversionType.Attribute => GetAttribute(prop, ref reader, tracker),
                ConversionType.ComplexArray => GetArray(prop, ref reader, tracker),
                ConversionType.SimpleArray => GetArray(prop, ref reader, tracker),
                ConversionType.ClassArray => GetArray(prop, ref reader, tracker),
                _ => throw new InvalidOperationException(nameof(ConversionType))
            });
        }

        return node;
    }

    private XElement GetArray(DataCorePropertyDefinition prop, ref SpanReader reader, Stack<(int, int)> tracker)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        var arrayNode = new XElement(prop.GetName(Database));

        for (var i = 0; i < count; i++)
        {
            var instanceIndex = firstIndex + i;

            arrayNode.Add(prop.DataType switch
            {
                DataType.Reference => GetFromReference(Database.ReferenceValues[instanceIndex], tracker),
                DataType.WeakPointer => GetFromPointer(Database.WeakValues[instanceIndex], tracker),
                DataType.StrongPointer => GetFromPointer(Database.StrongValues[instanceIndex], tracker),
                DataType.Class => GetFromInstance(prop.StructIndex, instanceIndex, tracker),

                DataType.EnumChoice => new XElement(prop.DataType.ToStringFast(), Database.EnumValues[instanceIndex].ToString(Database)),
                DataType.Guid => new XElement(prop.DataType.ToStringFast(), Database.GuidValues[instanceIndex].ToString()),
                DataType.Locale => new XElement(prop.DataType.ToStringFast(), Database.LocaleValues[instanceIndex].ToString(Database)),
                DataType.Double => new XElement(prop.DataType.ToStringFast(), Database.DoubleValues[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.Single => new XElement(prop.DataType.ToStringFast(), Database.SingleValues[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.String => new XElement(prop.DataType.ToStringFast(), Database.StringIdValues[instanceIndex].ToString(Database)),
                DataType.UInt64 => new XElement(prop.DataType.ToStringFast(), Database.UInt64Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.UInt32 => new XElement(prop.DataType.ToStringFast(), Database.UInt32Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.UInt16 => new XElement(prop.DataType.ToStringFast(), Database.UInt16Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.Byte => new XElement(prop.DataType.ToStringFast(), Database.UInt8Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.Int64 => new XElement(prop.DataType.ToStringFast(), Database.Int64Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.Int32 => new XElement(prop.DataType.ToStringFast(), Database.Int32Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.Int16 => new XElement(prop.DataType.ToStringFast(), Database.Int16Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.SByte => new XElement(prop.DataType.ToStringFast(), Database.Int8Values[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                DataType.Boolean => new XElement(prop.DataType.ToStringFast(), Database.BooleanValues[instanceIndex].ToString(CultureInfo.InvariantCulture)),
                _ => throw new InvalidOperationException(nameof(DataType))
            });
        }

        return arrayNode;
    }

    private XObject GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader, Stack<(int, int)> tracker)
    {
        return prop.DataType switch
        {
            DataType.Reference => GetFromReference(reader.Read<DataCoreReference>(), tracker),
            DataType.WeakPointer => GetFromPointer(reader.Read<DataCorePointer>(), tracker),
            DataType.StrongPointer => GetFromPointer(reader.Read<DataCorePointer>(), tracker),
            DataType.Class => GetFromStruct(prop.StructIndex, ref reader, tracker),

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

    private XElement GetFromReference(DataCoreReference reference, Stack<(int, int)> tracker)
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
            fileReferenceNode.Add(new XAttribute("__fileName", record.GetFileName(Database)));
            return fileReferenceNode;
        }

        return GetFromRecord(record, tracker);
    }

    public XElement GetFromRecord(DataCoreRecord record, Stack<(int, int)> tracker)
    {
        var element = GetFromInstance(record.StructIndex, record.InstanceIndex, tracker);
        element.Add(new XAttribute("__recordGuid", record.Id.ToString()));
        return element;
    }

    private XElement GetFromPointer(DataCorePointer pointer, Stack<(int, int)> tracker)
    {
        if (pointer.IsInvalid)
        {
            var invalidNode = new XElement("NullPointer");
            invalidNode.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
            invalidNode.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
            return invalidNode;
        }

        return GetFromInstance(pointer.StructIndex, pointer.InstanceIndex, tracker);
    }

    private XElement GetFromInstance(int structIndex, int instanceIndex, Stack<(int, int)> tracker)
    {
        if (tracker.Contains((structIndex, instanceIndex)))
        {
            var circularNode = new XElement("CircularReference");

            circularNode.Add(new XAttribute("__structName", Database.StructDefinitions[structIndex].GetName(Database)));
            circularNode.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
            circularNode.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));

            return circularNode;
        }

        tracker.Push((structIndex, instanceIndex));

        var reader = Database.GetReader(Database.Offsets[structIndex][instanceIndex]);
        var element = GetFromStruct(structIndex, ref reader, tracker);

        tracker.Pop();

        // add some metadata to the element, mostly so we can figure out what a CircularReference is pointing to
        element.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
        element.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));

        return element;
    }
}