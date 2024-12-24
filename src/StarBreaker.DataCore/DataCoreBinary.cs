using System.Diagnostics;
using System.IO.Enumeration;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinary
{
    private readonly Dictionary<(int, int), XmlNode> _cache = new();
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

    private void FillNode(XmlNode node, DataCoreStructDefinition structDef, ref SpanReader reader)
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

                //node.AppendChild(new XmlNode("__metadata"));
            }
        }
    }

    private void FillArray(XmlNode node, ref SpanReader reader, DataCorePropertyDefinition prop)
    {
        var count = reader.ReadUInt32();
        var firstIndex = reader.ReadUInt32();

        if (count == 0)
            return;

        var arrayNode = new XmlNode(prop.GetName(Database));

        for (var i = 0; i < count; i++)
        {
            var index = (int)firstIndex + i;

            XmlNode childNode;

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

                childNode = GetFromPointer((int)reference.StructIndex, (int)reference.InstanceIndex);
            }
            else if (prop.DataType == DataType.Reference)
            {
                var reference = Database.ReferenceValues[index];
                if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == 0xffffffff)
                {
                    arrayNode.AppendAttribute(new XmlAttribute<string>("null", "null"));
                    continue;
                }

                childNode = GetFromRecord(reference.RecordId);
            }
            else if (prop.DataType == DataType.Class)
            {
                childNode = GetFromPointer(prop.StructIndex, index);
            }
            else
            {
                childNode = GetArrayItemNode(prop, index);
            }

            arrayNode.AppendChild(childNode);
        }

        node.AppendChild(arrayNode);
    }

    private void FillAttribute(XmlNode node, ref SpanReader reader, DataCorePropertyDefinition prop)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement switch here looks kinda ugly idk
        if (prop.DataType == DataType.Class)
        {
            var structDef3 = Database.StructDefinitions[prop.StructIndex];

            var childClass = new XmlNode(prop.GetName(Database));

            FillNode(childClass, structDef3, ref reader);

            node.AppendChild(childClass);
        }
        else if (prop.DataType is DataType.StrongPointer or DataType.WeakPointer)
        {
            var ptr = reader.Read<DataCorePointer>();

            if (ptr.StructIndex == 0xFFFFFFFF || ptr.InstanceIndex == 0xFFFFFFFF)
            {
                node.AppendAttribute(new XmlAttribute<string>(prop.GetName(Database), "null"));
                return;
            }

            node.AppendChild(GetFromPointer((int)ptr.StructIndex, (int)ptr.InstanceIndex));
        }
        else if (prop.DataType == DataType.Reference)
        {
            var reference = reader.Read<DataCoreReference>();
            if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == 0xffffffff)
            {
                node.AppendAttribute(new XmlAttribute<string>(prop.GetName(Database), "null"));
                return;
            }

            node.AppendChild(GetFromRecord(reference.RecordId));
        }
        else
        {
            node.AppendAttribute(GetAttribute(prop, ref reader));
        }
    }

    public XmlNode GetFromRecord(CigGuid guid)
    {
        var record = Database.GetRecord(guid);

        // var node = new XmlNode("Record");
        // node.AppendAttribute(new XmlAttribute<string>("__name", record.GetName(Database)));
        // node.AppendAttribute(new XmlAttribute<string>("__fileName", record.GetFileName(Database)));
        // node.AppendAttribute(new XmlAttribute<string>("__guid", guid.ToString()));
        // return node;

        return GetFromPointer(record.StructIndex, record.InstanceIndex);
    }

    private XmlNode GetFromPointer(int structIndex, int instanceIndex)
    {
        if (_cache.TryGetValue((structIndex, instanceIndex), out var node))
            return node;

        var structDef = Database.StructDefinitions[structIndex];
        var offset = Database.Offsets[structIndex][instanceIndex];
        var reader = Database.GetReader(offset);

        node = new XmlNode(structDef.GetName(Database));

        //store the node in the cache before filling it to prevent infinite recursion
        _cache[(structIndex, instanceIndex)] = node;

        FillNode(node, structDef, ref reader);

        return node;
    }

    private XmlNode GetArrayItemNode(DataCorePropertyDefinition prop, int index)
    {
        var arrayItem = new XmlNode(prop.DataType.ToStringFast());

        const string indexName = "__index";
        XmlAttribute att = prop.DataType switch
        {
            DataType.Byte => new XmlAttribute<byte>(indexName, Database.UInt8Values[index]),
            DataType.Int16 => new XmlAttribute<short>(indexName, Database.Int16Values[index]),
            DataType.Int32 => new XmlAttribute<int>(indexName, Database.Int32Values[index]),
            DataType.Int64 => new XmlAttribute<long>(indexName, Database.Int64Values[index]),
            DataType.SByte => new XmlAttribute<sbyte>(indexName, Database.Int8Values[index]),
            DataType.UInt16 => new XmlAttribute<ushort>(indexName, Database.UInt16Values[index]),
            DataType.UInt32 => new XmlAttribute<uint>(indexName, Database.UInt32Values[index]),
            DataType.UInt64 => new XmlAttribute<ulong>(indexName, Database.UInt64Values[index]),
            DataType.Boolean => new XmlAttribute<bool>(indexName, Database.BooleanValues[index]),
            DataType.Single => new XmlAttribute<float>(indexName, Database.SingleValues[index]),
            DataType.Double => new XmlAttribute<double>(indexName, Database.DoubleValues[index]),
            DataType.Guid => new XmlAttribute<CigGuid>(indexName, Database.GuidValues[index]),
            DataType.String => new XmlAttribute<string>(indexName, Database.GetString(Database.StringIdValues[index])),
            DataType.Locale => new XmlAttribute<string>(indexName, Database.GetString(Database.LocaleValues[index])),
            DataType.EnumChoice => new XmlAttribute<string>(indexName, Database.GetString(Database.EnumValues[index])),
            _ => throw new InvalidOperationException(nameof(DataType))
        };

        arrayItem.AppendAttribute(att);

        return arrayItem;
    }

    private XmlAttribute GetAttribute(DataCorePropertyDefinition prop, ref SpanReader reader)
    {
        var name = prop.GetName(Database);
        
        return prop.DataType switch
        {
            DataType.Boolean => new XmlAttribute<bool>(name, reader.ReadBoolean()),
            DataType.Single => new XmlAttribute<float>(name, reader.ReadSingle()),
            DataType.Double => new XmlAttribute<double>(name, reader.ReadDouble()),
            DataType.Guid => new XmlAttribute<CigGuid>(name, reader.Read<CigGuid>()),
            DataType.SByte => new XmlAttribute<sbyte>(name, reader.ReadSByte()),
            DataType.UInt16 => new XmlAttribute<ushort>(name, reader.ReadUInt16()),
            DataType.UInt32 => new XmlAttribute<uint>(name, reader.ReadUInt32()),
            DataType.UInt64 => new XmlAttribute<ulong>(name, reader.ReadUInt64()),
            DataType.Byte => new XmlAttribute<byte>(name, reader.ReadByte()),
            DataType.Int16 => new XmlAttribute<short>(name, reader.ReadInt16()),
            DataType.Int32 => new XmlAttribute<int>(name, reader.ReadInt32()),
            DataType.Int64 => new XmlAttribute<long>(name, reader.ReadInt64()),
            DataType.String or DataType.Locale or DataType.EnumChoice => new XmlAttribute<string>(name, Database.GetString(reader.Read<DataCoreStringId>())),
            _ => throw new UnreachableException()
        };
    }
}