using System.Diagnostics;
using System.Globalization;
using System.IO.Enumeration;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinary
{
    private readonly Dictionary<(int, int), XElement> _cache = new();
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

    private void FillNode(XElement node, DataCoreStructDefinition structDef, ref SpanReader reader)
    {
        foreach (ref readonly var prop in structDef.EnumerateProperties(Database.StructDefinitions, Database.PropertyDefinitions).AsSpan())
        {
            if (prop.ConversionType == ConversionType.Attribute)
            {
                FillAttribute(node, ref reader, prop);
            }
            else
            {
                FillArray(node, ref reader, prop);
            }
        }
    }

    private void FillArray(XElement node, ref SpanReader reader, DataCorePropertyDefinition prop)
    {
        var count = reader.ReadUInt32();
        var firstIndex = reader.ReadUInt32();

        if (count == 0)
            return;

        var arrayNode = new XElement(prop.GetName(Database));

        for (var i = 0; i < count; i++)
        {
            var index = (int)firstIndex + i;

            if (prop.DataType is DataType.StrongPointer or DataType.WeakPointer)
            {
                var reference = prop.DataType switch
                {
                    DataType.StrongPointer => Database.StrongValues[index],
                    DataType.WeakPointer => Database.WeakValues[index],
                    _ => throw new InvalidOperationException(nameof(DataType))
                };

                if (reference.StructIndex == 0xFFFFFFFF || reference.InstanceIndex == 0xFFFFFFFF)
                    continue;

                arrayNode.Add(GetFromPointer((int)reference.StructIndex, (int)reference.InstanceIndex));
            }
            else if (prop.DataType == DataType.Reference)
            {
                var reference = Database.ReferenceValues[index];
                if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == 0xffffffff)
                    continue;

                arrayNode.Add(GetReference(reference));
            }
            else if (prop.DataType == DataType.Class)
            {
                arrayNode.Add(GetFromPointer(prop.StructIndex, index));
            }
            else
            {
                arrayNode.Add(GetArrayItemNode(prop, index));
            }
        }

        node.Add(arrayNode);
    }

    private void FillAttribute(XElement node, ref SpanReader reader, DataCorePropertyDefinition prop)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement switch here looks kinda ugly idk
        if (prop.DataType == DataType.Class)
        {
            var structDef3 = Database.StructDefinitions[prop.StructIndex];

            var childClass = new XElement(prop.GetName(Database));

            FillNode(childClass, structDef3, ref reader);

            node.Add(childClass);
        }
        else if (prop.DataType is DataType.StrongPointer or DataType.WeakPointer)
        {
            var ptr = reader.Read<DataCorePointer>();

            if (ptr.StructIndex == 0xFFFFFFFF || ptr.InstanceIndex == 0xFFFFFFFF)
                return;

            node.Add(GetFromPointer((int)ptr.StructIndex, (int)ptr.InstanceIndex));
        }
        else if (prop.DataType == DataType.Reference)
        {
            var reference = reader.Read<DataCoreReference>();
            if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == 0xffffffff)
                return;

            node.Add(GetReference(reference));
        }
        else
        {
            node.Add(GetAttribute(prop, ref reader));
        }
    }

    public XElement GetReference(DataCoreReference reference)
    {
        var record = Database.GetRecord(reference.RecordId);

        //TODO: stack overflow here sometimes, need to investigate
        // return GetFromRecord(record);


        // this here is a workaround to prevent stack overflow.
        // At least tells the reader what the reference would have been.
        var node = new XElement("RecordReference");

        node.Add(new XAttribute("__name", record.GetName(Database)));
        node.Add(new XAttribute("__fileName", record.GetFileName(Database)));
        node.Add(new XAttribute("__guid", reference.RecordId.ToString()));

        return node;
    }

    public XElement GetFromRecord(DataCoreRecord record)
    {
        return GetFromPointer(record.StructIndex, record.InstanceIndex);
    }

    private XElement GetFromPointer(int structIndex, int instanceIndex)
    {
        if (_cache.TryGetValue((structIndex, instanceIndex), out var node))
            return node;

        var structDef = Database.StructDefinitions[structIndex];
        var offset = Database.Offsets[structIndex][instanceIndex];
        var reader = Database.GetReader(offset);

        node = new XElement(structDef.GetName(Database));

        //store the node in the cache before filling it to prevent infinite recursion
        _cache[(structIndex, instanceIndex)] = node;

        FillNode(node, structDef, ref reader);

        return node;
    }

    private XElement GetArrayItemNode(DataCorePropertyDefinition prop, int index)
    {
        var arrayItem = new XElement(prop.DataType.ToStringFast());

        const string indexName = "__index";
        var att = prop.DataType switch
        {
            DataType.Byte => new XAttribute(indexName, Database.UInt8Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Int16 => new XAttribute(indexName, Database.Int16Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Int32 => new XAttribute(indexName, Database.Int32Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Int64 => new XAttribute(indexName, Database.Int64Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.SByte => new XAttribute(indexName, Database.Int8Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.UInt16 => new XAttribute(indexName, Database.UInt16Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.UInt32 => new XAttribute(indexName, Database.UInt32Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.UInt64 => new XAttribute(indexName, Database.UInt64Values[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Boolean => new XAttribute(indexName, Database.BooleanValues[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Single => new XAttribute(indexName, Database.SingleValues[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Double => new XAttribute(indexName, Database.DoubleValues[index].ToString(CultureInfo.InvariantCulture)),
            DataType.Guid => new XAttribute(indexName, Database.GuidValues[index].ToString()),
            DataType.String => new XAttribute(indexName, Database.GetString(Database.StringIdValues[index])),
            DataType.Locale => new XAttribute(indexName, Database.GetString(Database.LocaleValues[index])),
            DataType.EnumChoice => new XAttribute(indexName, Database.GetString(Database.EnumValues[index])),
            _ => throw new InvalidOperationException(nameof(DataType))
        };

        arrayItem.Add(att);

        return arrayItem;
    }

    private XAttribute GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader)
    {
        var name = prop.GetName(Database);

        return prop.DataType switch
        {
            DataType.Boolean => new XAttribute(name, reader.ReadBoolean().ToString(CultureInfo.InvariantCulture)),
            DataType.Single => new XAttribute(name, reader.ReadSingle().ToString(CultureInfo.InvariantCulture)),
            DataType.Double => new XAttribute(name, reader.ReadDouble().ToString(CultureInfo.InvariantCulture)),
            DataType.Guid => new XAttribute(name, reader.Read<CigGuid>().ToString()),
            DataType.SByte => new XAttribute(name, reader.ReadSByte().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt16 => new XAttribute(name, reader.ReadUInt16().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt32 => new XAttribute(name, reader.ReadUInt32().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt64 => new XAttribute(name, reader.ReadUInt64().ToString(CultureInfo.InvariantCulture)),
            DataType.Byte => new XAttribute(name, reader.ReadByte().ToString(CultureInfo.InvariantCulture)),
            DataType.Int16 => new XAttribute(name, reader.ReadInt16().ToString(CultureInfo.InvariantCulture)),
            DataType.Int32 => new XAttribute(name, reader.ReadInt32().ToString(CultureInfo.InvariantCulture)),
            DataType.Int64 => new XAttribute(name, reader.ReadInt64().ToString(CultureInfo.InvariantCulture)),
            DataType.String or DataType.Locale or DataType.EnumChoice => new XAttribute(name, Database.GetString(reader.Read<DataCoreStringId>())),
            //DataType.Reference => new XAttribute(name, reader.Read<DataCoreReference>().ToString()),
            //DataType.StrongPointer or DataType.WeakPointer => new XAttribute(name, reader.Read<DataCorePointer>().ToString()),
            DataType.Class => throw new InvalidOperationException("Classes should be handled by FillNode"),
            _ => throw new InvalidOperationException(nameof(DataType))
        };
    }
}