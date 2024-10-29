using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using StarBreaker.Common;

namespace StarBreaker.Forge;

public sealed class DataForge
{
    public readonly Database _database;
    private readonly Dictionary<int, int[]> _offsets;
    
    public Database Database => _database;

    public DataForge(Stream fs)
    {
        _database = new Database(fs, out var bytesRead);
        _offsets = ReadOffsets(bytesRead);
    }

    private Dictionary<int, int[]> ReadOffsets(int initialOffset)
    {
        var instances = new Dictionary<int, int[]>();

        foreach (var mapping in _database.DataMappings)
        {
            var arr = new int[mapping.StructCount];
            var structDef = _database.StructDefinitions[mapping.StructIndex];
            var structSize = structDef.CalculateSize(_database.StructDefinitions, _database.PropertyDefinitions);

            for (var i = 0; i < mapping.StructCount; i++)
            {
                arr[i] = initialOffset;
                initialOffset += structSize;
            }

            instances.Add(mapping.StructIndex, arr);
        }

        return instances;
    }

    public Dictionary<string, string[]> ExportEnums()
    {
        var result = new Dictionary<string, string[]>(_database.EnumDefinitions.Length);

        foreach (var enumDef in _database.EnumDefinitions)
        {
            var enumValues = new string[enumDef.ValueCount];
            for (var i = 0; i < enumDef.ValueCount; i++)
            {
                enumValues[i] = _database.GetString2(_database.EnumOptions[enumDef.FirstValueIndex + i]);
            }

            result.Add(enumDef.GetName(_database), enumValues);
        }

        return result;
    }

    public Dictionary<string, DataForgeRecord> GetRecordsByFileName(string? fileNameFilter = null)
    {
        var structsPerFileName = new Dictionary<string, DataForgeRecord>();
        foreach (var record in _database.RecordDefinitions)
        {
            var fileName = record.GetFileName(_database);

            if (fileNameFilter != null && !FileSystemName.MatchesSimpleExpression(fileNameFilter, fileName, true))
                continue;

            //this looks a lil wonky, but it's correct.
            //we will either find only on record for any given name,
            //or when we find multiple, we only care about the last one.
            structsPerFileName[fileName] = record;
        }

        return structsPerFileName;
    }

    public void ExtractSingleRecord(TextWriter writer, DataForgeRecord record)
    {
        var structDef = _database.StructDefinitions[record.StructIndex];
        var offset = _offsets[record.StructIndex][record.InstanceIndex];

        var reader = _database.GetReader(offset);
        
        var node = new XmlNode("Record");
        node.AppendAttribute(new XmlAttribute<string>("__type", structDef.GetName(_database)));
        node.AppendAttribute(new XmlAttribute<string>("__guid", record.Hash.ToString()));
        
        FillNode(node, structDef, ref reader);

        node.WriteTo(writer, 0);
    }

    public void Extract(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        Parallel.ForEach(recordsByFileName, kvp =>
        {
            var (fileName, record) = kvp;

            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            {
                using var writer = new StreamWriter(filePath);

                ExtractSingleRecord(writer, record);
            }

            lock (recordsByFileName)
            {
                var currentProgress = Interlocked.Increment(ref progressValue);
                //only report progress every 250 records and when we are done
                if (currentProgress == total || currentProgress % 250 == 0)
                    progress?.Report(currentProgress / (double)total);
            }
        });
    }

    private void FillNode(XmlNode node, DataForgeStructDefinition structDef, ref SpanReader reader)
    {
        foreach (ref readonly var prop in structDef.EnumerateProperties(_database.StructDefinitions, _database.PropertyDefinitions).AsSpan())
        {
            if (prop.ConversionType == ConversionType.Attribute)
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement switch here looks kinda ugly idk
                if (prop.DataType == DataType.Class)
                {
                    var structDef3 = _database.StructDefinitions[prop.StructIndex];

                    var childClass = new XmlNode(prop.GetName(_database));

                    FillNode(childClass, structDef3, ref reader);

                    node.AppendChild(childClass);
                }
                else if (prop.DataType is DataType.StrongPointer /* or DataType.WeakPointer*/)
                {
                    var ptr = reader.Read<DataForgePointer>();

                    if (ptr.StructIndex == 0xFFFFFFFF || ptr.InstanceIndex == 0xFFFFFFFF) continue;

                    var structDef2 = _database.StructDefinitions[(int)ptr.StructIndex];
                    var offset2 = _offsets[(int)ptr.StructIndex][(int)ptr.InstanceIndex];

                    var reader2 = _database.GetReader(offset2);

                    var child = new XmlNode(prop.GetName(_database));

                    FillNode(child, structDef2, ref reader2);

                    node.AppendChild(child);
                }
                else
                {
                    var name1 = prop.GetName(_database);
                    switch (prop.DataType)
                    {
                        case DataType.Boolean:
                            node.AppendAttribute(new XmlAttribute<bool>(name1, reader.ReadBoolean()));
                            break;
                        case DataType.Single:
                            node.AppendAttribute(new XmlAttribute<float>(name1, reader.ReadSingle()));
                            break;
                        case DataType.Double:
                            node.AppendAttribute(new XmlAttribute<double>(name1, reader.ReadDouble()));
                            break;
                        case DataType.Guid:
                            node.AppendAttribute(new XmlAttribute<CigGuid>(name1, reader.Read<CigGuid>()));
                            break;
                        case DataType.SByte:
                            node.AppendAttribute(new XmlAttribute<sbyte>(name1, reader.ReadSByte()));
                            break;
                        case DataType.UInt16:
                            node.AppendAttribute(new XmlAttribute<ushort>(name1, reader.ReadUInt16()));
                            break;
                        case DataType.UInt32:
                            node.AppendAttribute(new XmlAttribute<uint>(name1, reader.ReadUInt32()));
                            break;
                        case DataType.UInt64:
                            node.AppendAttribute(new XmlAttribute<ulong>(name1, reader.ReadUInt64()));
                            break;
                        case DataType.Byte:
                            node.AppendAttribute(new XmlAttribute<byte>(name1, reader.ReadByte()));
                            break;
                        case DataType.Int16:
                            node.AppendAttribute(new XmlAttribute<short>(name1, reader.ReadInt16()));
                            break;
                        case DataType.Int32:
                            node.AppendAttribute(new XmlAttribute<int>(name1, reader.ReadInt32()));
                            break;
                        case DataType.Int64:
                            node.AppendAttribute(new XmlAttribute<long>(name1, reader.ReadInt64()));
                            break;
                        case DataType.Reference:
                            node.AppendAttribute(new XmlAttribute<DataForgeReference>(name1, reader.Read<DataForgeReference>()));
                            break;
                        case DataType.String:
                            node.AppendAttribute(new XmlAttribute<string>(name1, _database.GetString(reader.Read<DataForgeStringId>())));
                            break;
                        case DataType.Locale:
                            node.AppendAttribute(new XmlAttribute<string>(name1, _database.GetString(reader.Read<DataForgeStringId>())));
                            break;
                        case DataType.EnumChoice:
                            node.AppendAttribute(new XmlAttribute<string>(name1, _database.GetString(reader.Read<DataForgeStringId>())));
                            break;
                        case DataType.WeakPointer:
                            node.AppendAttribute(new XmlAttribute<DataForgePointer>(name1, reader.Read<DataForgePointer>()));
                            break;
                        default:
                            throw new UnreachableException();
                    }
                }
            }
            else
            {
                var count = reader.ReadUInt32();
                var firstIndex = reader.ReadUInt32();

                var arrayNode = new XmlNode(prop.GetName(_database));
                //arrayNode.AppendAttribute(new XmlAttribute<uint>("__count__", count));

                for (var i = 0; i < count; i++)
                {
                    var index = (int)firstIndex + i;

                    if (prop.DataType == DataType.Class)
                    {
                        var structDef1 = _database.StructDefinitions[prop.StructIndex];
                        var offset1 = _offsets[prop.StructIndex][index];
                        var reader1 = _database.GetReader(offset1);

                        var child = new XmlNode(structDef1.GetName(_database));

                        FillNode(child, structDef1, ref reader1);

                        arrayNode.AppendChild(child);
                    }
                    else if (prop.DataType is DataType.StrongPointer /*or DataType.WeakPointer*/)
                    {
                        var reference = prop.DataType switch
                        {
                            DataType.StrongPointer => _database.StrongValues[index],
                            DataType.WeakPointer => _database.WeakValues[index],
                            _ => throw new InvalidOperationException(nameof(DataType))
                        };

                        if (reference.StructIndex == 0xFFFFFFFF || reference.InstanceIndex == 0xFFFFFFFF) continue;

                        var structDef2 = _database.StructDefinitions[(int)reference.StructIndex];
                        var offset2 = _offsets[(int)reference.StructIndex][(int)reference.InstanceIndex];
                        var reader2 = _database.GetReader(offset2);

                        var child = new XmlNode(prop.GetName(_database));

                        FillNode(child, structDef2, ref reader2);

                        arrayNode.AppendChild(child);
                    }
                    else
                    {
                        var arrayItem = new XmlNode(prop.DataType.ToStringFast());

                        switch (prop.DataType)
                        {
                            case DataType.Byte:
                                arrayItem.AppendAttribute(new XmlAttribute<byte>("__value", _database.UInt8Values[index]));
                                break;
                            case DataType.Int16:
                                arrayItem.AppendAttribute(new XmlAttribute<short>("__value", _database.Int16Values[index]));
                                break;
                            case DataType.Int32:
                                arrayItem.AppendAttribute(new XmlAttribute<int>("__value", _database.Int32Values[index]));
                                break;
                            case DataType.Int64:
                                arrayItem.AppendAttribute(new XmlAttribute<long>("__value", _database.Int64Values[index]));
                                break;
                            case DataType.SByte:
                                arrayItem.AppendAttribute(new XmlAttribute<sbyte>("__value", _database.Int8Values[index]));
                                break;
                            case DataType.UInt16:
                                arrayItem.AppendAttribute(new XmlAttribute<ushort>("__value", _database.UInt16Values[index]));
                                break;
                            case DataType.UInt32:
                                arrayItem.AppendAttribute(new XmlAttribute<uint>("__value", _database.UInt32Values[index]));
                                break;
                            case DataType.UInt64:
                                arrayItem.AppendAttribute(new XmlAttribute<ulong>("__value", _database.UInt64Values[index]));
                                break;
                            case DataType.Boolean:
                                arrayItem.AppendAttribute(new XmlAttribute<bool>("__value", _database.BooleanValues[index]));
                                break;
                            case DataType.Single:
                                arrayItem.AppendAttribute(new XmlAttribute<float>("__value", _database.SingleValues[index]));
                                break;
                            case DataType.Double:
                                arrayItem.AppendAttribute(new XmlAttribute<double>("__value", _database.DoubleValues[index]));
                                break;
                            case DataType.Guid:
                                arrayItem.AppendAttribute(new XmlAttribute<CigGuid>("__value", _database.GuidValues[index]));
                                break;
                            case DataType.String:
                                arrayItem.AppendAttribute(new XmlAttribute<string>("__value", _database.GetString(_database.StringIdValues[index])));
                                break;
                            case DataType.Locale:
                                arrayItem.AppendAttribute(new XmlAttribute<string>("__value", _database.GetString(_database.LocaleValues[index])));
                                break;
                            case DataType.EnumChoice:
                                arrayItem.AppendAttribute(new XmlAttribute<string>("__value", _database.GetString(_database.EnumValues[index])));
                                break;
                            case DataType.Reference:
                                arrayItem.AppendAttribute(new XmlAttribute<DataForgeReference>("__value", _database.ReferenceValues[index]));
                                break;
                            case DataType.WeakPointer:
                                arrayItem.AppendAttribute(new XmlAttribute<DataForgePointer>("__value", _database.StrongValues[index]));
                                break;
                            case DataType.Class:
                                arrayItem.AppendAttribute(new XmlAttribute<DataForgePointer>("__value", _database.StrongValues[index]));
                                break;
                            default:
                                throw new InvalidOperationException(nameof(DataType));
                        }

                        arrayNode.AppendChild(arrayItem);
                    }
                }

                node.AppendChild(arrayNode);
            }
        }
    }

    //this is bad, fix
    public void ExtractSingle(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var total = _database.RecordDefinitions.Length;
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);
        using var writer = new StreamWriter(Path.Combine(outputFolder, "StarBreaker.Export.xml"), false, Encoding.UTF8, 1024 * 1024);
        writer.WriteLine("<__root>");

        foreach (var record in _database.RecordDefinitions)
        {
            if (fileNameFilter != null)
            {
                var fileName = record.GetFileName(_database);

                if (!FileSystemName.MatchesSimpleExpression(fileNameFilter, fileName, true))
                    continue;
            }

            var structDef = _database.StructDefinitions[record.StructIndex];
            var offset = _offsets[record.StructIndex][record.InstanceIndex];
            var reader = _database.GetReader(offset);
            var child = new XmlNode(structDef.GetName(_database));

            FillNode(child, structDef, ref reader);

            child.WriteTo(writer, 1);

            ++progressValue;
            if (progressValue % 250 == 0 || progressValue == total)
                progress?.Report(progressValue / (double)total);
        }

        writer.WriteLine("</__root>");
    }
}