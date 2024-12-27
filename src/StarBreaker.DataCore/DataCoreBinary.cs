using System.Globalization;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class VisitedTracker
{
    private readonly HashSet<(int, int)> _visited = [];

    public bool TryVisit(int structIndex, int instanceIndex) => _visited.Add((structIndex, instanceIndex));
}

public sealed class DataCoreBinary
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinary(Stream fs)
    {
        Database = new DataCoreDatabase(fs);
    }

    private XElement GetFromStruct(int structIndex, ref SpanReader reader, VisitedTracker tracker)
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

    private XElement GetArray(DataCorePropertyDefinition prop, ref SpanReader reader, VisitedTracker tracker)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        var arrayNode = new XElement(prop.GetName(Database));

        for (var i = 0; i < count; i++)
        {
            var instanceIndex = firstIndex + i;

            arrayNode.Add(prop.DataType switch
            {
                DataType.Reference => GetReference(Database.ReferenceValues[instanceIndex], tracker),
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

        if (count == 0)
            arrayNode.Add(new XAttribute("Empty", "true"));

        return arrayNode;
    }

    private XObject GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader, VisitedTracker tracker)
    {
        return prop.DataType switch
        {
            DataType.Reference => GetReference(reader.Read<DataCoreReference>(), tracker),
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

    private XElement GetReference(DataCoreReference reference, VisitedTracker tracker)
    {
        if (reference.IsInvalid)
        {
            var invalidNode = new XElement("InvalidReference");
            invalidNode.Add(new XAttribute("__guid", reference.RecordId.ToString()));
            return invalidNode;
        }

        var record = Database.GetRecord(reference.RecordId);

        return GetFromInstance(record.StructIndex, record.InstanceIndex, tracker);
    }

    private XElement GetFromPointer(DataCorePointer pointer, VisitedTracker tracker)
    {
        if (pointer.IsInvalid)
        {
            var invalidNode = new XElement("InvalidPointer");
            invalidNode.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
            invalidNode.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
            return invalidNode;
        }

        return GetFromInstance(pointer.StructIndex, pointer.InstanceIndex, tracker);
    }

    public XElement GetFromRecord(DataCoreRecord record)
    {
        return GetFromInstance(record.StructIndex, record.InstanceIndex, new VisitedTracker());
    }

    private XElement GetFromInstance(int structIndex, int instanceIndex, VisitedTracker tracker)
    {
        if (!tracker.TryVisit(structIndex, instanceIndex))
        {
            var circularNode = new XElement("CircularReference");
            circularNode.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
            circularNode.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));
            circularNode.Add(new XAttribute("__structName", Database.StructDefinitions[structIndex].GetName(Database)));
            return circularNode;
        }

        var reader = Database.GetReader(Database.Offsets[structIndex][instanceIndex]);
        return GetFromStruct(structIndex, ref reader, tracker);
    }
}