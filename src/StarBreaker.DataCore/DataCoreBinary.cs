using System.Globalization;
using System.IO.Enumeration;
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

    public Dictionary<string, DataCoreRecord> GetRecordsByFileName(string? fileNameFilter = null)
    {
        var structsPerFileName = new Dictionary<string, DataCoreRecord>();
        foreach (var record in Database.RecordDefinitions)
        {
            var fileName = record.GetFileName(Database);

            if (fileNameFilter != null && !FileSystemName.MatchesSimpleExpression(fileNameFilter, fileName))
                continue;

            //this looks a lil wonky, but it's correct.
            //we will either find only on record for any given name,
            //or when we find multiple, we only care about the last one.
            structsPerFileName[fileName] = record;
        }

        return structsPerFileName;
    }

    private XElement GetFromStruct(int structIndex, ref SpanReader reader)
    {
        var node = new XElement(Database.StructDefinitions[structIndex].GetName(Database));

        foreach (var prop in Database.GetProperties(structIndex))
        {
            node.Add(prop.ConversionType switch
            {
                //TODO: do we need to handle different types of arrays?
                ConversionType.Attribute => GetAttribute(prop, ref reader),
                ConversionType.ComplexArray => GetArray(prop, ref reader),
                ConversionType.SimpleArray => GetArray(prop, ref reader),
                ConversionType.ClassArray => GetArray(prop, ref reader),
                _ => throw new InvalidOperationException(nameof(ConversionType))
            });
        }

        return node;
    }

    private XElement GetArray(DataCorePropertyDefinition prop, ref SpanReader reader)
    {
        var count = reader.ReadUInt32();
        var firstIndex = reader.ReadInt32();
        var arrayNode = new XElement(prop.GetName(Database));

        for (var i = 0; i < count; i++)
        {
            var instanceIndex = firstIndex + i;

            arrayNode.Add(prop.DataType switch
            {
                DataType.Reference => CreateSimpleReference(Database.ReferenceValues[instanceIndex]),
                DataType.WeakPointer => CreateSimplePointer(Database.WeakValues[instanceIndex], "WeakPointer"),
                DataType.StrongPointer => GetFromPointer(Database.StrongValues[instanceIndex]),
                DataType.Class => GetFromInstance(prop.StructIndex, instanceIndex),

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

    private XObject GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader)
    {
        return prop.DataType switch
        {
            DataType.Reference => CreateSimpleReference(reader.Read<DataCoreReference>()),
            DataType.WeakPointer => CreateSimplePointer(reader.Read<DataCorePointer>(), "WeakPointer"),
            DataType.StrongPointer => GetFromPointer(reader.Read<DataCorePointer>()),
            DataType.Class => GetFromStruct(prop.StructIndex, ref reader),

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

    public XElement GetReference(DataCoreReference reference)
    {
        if (reference.IsInvalid)
            return CreateSimpleReference(reference);

        var record = Database.GetRecord(reference.RecordId);
        return GetFromInstance(record.StructIndex, record.InstanceIndex);
    }

    public XElement GetFromPointer(DataCorePointer pointer)
    {
        if (pointer.IsInvalid)
            return CreateSimplePointer(pointer, "InvalidPointer");

        return GetFromInstance(pointer.StructIndex, pointer.InstanceIndex);
    }


    public XElement GetFromRecord(DataCoreRecord record)
    {
        // use this from outside. create a stack and pass it along for circular reference checking.
        var stack = new Stack<CigGuid>();

        var node = GetFromInstance(record.StructIndex, record.InstanceIndex);

        return node;
    }

    //cache these?
    public XElement GetFromInstance(int structIndex, int instanceIndex)
    {
        var reader = Database.GetReader(Database.Offsets[structIndex][instanceIndex]);

        return GetFromStruct(structIndex, ref reader);
    }

    private XElement CreateSimpleReference(DataCoreReference reference)
    {
        if (reference.IsInvalid)
        {
            var invalidNode = new XElement("InvalidReference");
            invalidNode.Add(new XAttribute("__guid", reference.RecordId.ToString()));
            return invalidNode;
        }

        var record = Database.GetRecord(reference.RecordId);
        var node = new XElement("RecordReference");

        node.Add(new XAttribute("__name", record.GetName(Database)));
        node.Add(new XAttribute("__fileName", record.GetFileName(Database)));
        node.Add(new XAttribute("__guid", reference.RecordId.ToString()));
        node.Add(new XAttribute("__structName", Database.StructDefinitions[record.StructIndex].GetName(Database)));
        node.Add(new XAttribute("__structIndex", record.StructIndex.ToString(CultureInfo.InvariantCulture)));
        node.Add(new XAttribute("__instanceIndex", record.InstanceIndex.ToString(CultureInfo.InvariantCulture)));

        return node;
    }

    private static XElement CreateSimplePointer(DataCorePointer pointer, string name)
    {
        if (pointer.IsInvalid)
        {
            var invalidNode = new XElement("Invalid" + name);
            invalidNode.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
            invalidNode.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));
            return invalidNode;
        }

        var node = new XElement(name);

        node.Add(new XAttribute("__structIndex", pointer.StructIndex.ToString(CultureInfo.InvariantCulture)));
        node.Add(new XAttribute("__instanceIndex", pointer.InstanceIndex.ToString(CultureInfo.InvariantCulture)));

        return node;
    }
}