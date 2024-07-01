using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using StarBreaker.Common;

namespace StarBreaker.Forge;

public sealed class DataForge : IDataForge
{
    public readonly Database _database;
    private readonly Dictionary<int, int[]> _offsets;

    public DataForge(string dcb)
    {
        _database = new Database(dcb, out var bytesRead);
        _offsets = ReadOffsets(bytesRead);
    }

    public DataForge(ReadOnlySpan<byte> allBytes)
    {
        _database = new Database(allBytes, out var bytesRead);
        _offsets = ReadOffsets(bytesRead);
    }

    public void Extract(string outputFolder, Regex? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var structsPerFileName = new Dictionary<string, List<DataForgeRecord>>();
        foreach (var record in _database.RecordDefinitions)
        {
            var s = _database.GetString(record.FileNameOffset);

            //TODO: use FileSystemName.MatchesSimpleExpression() instead of regex
            if (fileNameFilter != null && !fileNameFilter.IsMatch(s))
                continue;

            if (!structsPerFileName.TryGetValue(s, out var list))
            {
                list = [];
                structsPerFileName.Add(s, list);
            }

            list.Add(record);
        }

        var total = structsPerFileName.Count;

        Parallel.ForEach(structsPerFileName, data =>
        {
            var structs = _database.StructDefinitions.AsSpan();
            var properties = _database.PropertyDefinitions.AsSpan();
            var filePath = Path.Combine(outputFolder, data.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            if (data.Value.Count == 1)
            {
                using var writer = new StreamWriter(filePath);

                var record = data.Value[0];
                var structDef = _database.StructDefinitions[record.StructIndex];
                var offset = _offsets[record.StructIndex][record.InstanceIndex];

                var reader = _database.GetReader(offset);

                var node = new XmlNode(_database.GetString(structDef.NameOffset));

                FillNode(node, structDef, ref reader);

                node.WriteTo(writer, 0);
            }
            else
            {
                using var writer = new StreamWriter(filePath);

                writer.Write('<');
                writer.Write("__root");
                writer.Write('>');
                writer.WriteLine();

                foreach (var record in data.Value)
                {
                    var structDef = structs[record.StructIndex];
                    var offset = _offsets[record.StructIndex][record.InstanceIndex];

                    var reader = _database.GetReader(offset);

                    var node = new XmlNode(_database.GetString(structDef.NameOffset));

                    FillNode(node, structDef, ref reader);

                    node.WriteTo(writer, 1);
                }

                writer.WriteLine();
                writer.Write("</");
                writer.Write("__root");
                writer.Write('>');
            }

            lock (structsPerFileName)
            {
                var currentProgress = Interlocked.Increment(ref progressValue);
                //only report progress every 250 records and when we are done
                if (currentProgress == total || currentProgress % 250 == 0)
                    progress?.Report(currentProgress / (double)total);
            }
        });
    }

    public void ExtractSingle(string outputFolder, Regex? fileNameFilter = null, IProgress<double>? progress = null)
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
                var s = _database.GetString(record.FileNameOffset);

                //TODO: use FileSystemName.MatchesSimpleExpression() instead of regex
                if (!fileNameFilter.IsMatch(s))
                    continue;
            }

            var structDef = _database.StructDefinitions[record.StructIndex];
            var offset = _offsets[record.StructIndex][record.InstanceIndex];
            var reader = _database.GetReader(offset);
            var child = new XmlNode(_database.GetString(structDef.NameOffset));

            FillNode(child, structDef, ref reader);

            child.WriteTo(writer, 1);

            ++progressValue;
            if (progressValue % 250 == 0 || progressValue == total)
                progress?.Report(progressValue / (double)total);
        }

        writer.WriteLine("</__root>");
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

                    var childClass = new XmlNode(_database.GetString(prop.NameOffset));

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

                    var child = new XmlNode(_database.GetString(prop.NameOffset));

                    FillNode(child, structDef2, ref reader2);

                    node.AppendChild(child);
                }
                else
                {
                    var name1 = _database.GetString(prop.NameOffset);
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

                var arrayNode = new XmlNode(_database.GetString(prop.NameOffset));
                arrayNode.AppendAttribute(new XmlAttribute<uint>("__count__", count));

                for (var i = 0; i < count; i++)
                {
                    var index = (int)firstIndex + i;

                    if (prop.DataType == DataType.Class)
                    {
                        var structDef1 = _database.StructDefinitions[prop.StructIndex];
                        var offset1 = _offsets[prop.StructIndex][index];
                        var reader1 = _database.GetReader(offset1);

                        var child = new XmlNode(_database.GetString(structDef1.NameOffset));

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

                        var child = new XmlNode(_database.GetString(prop.NameOffset));

                        FillNode(child, structDef2, ref reader2);

                        arrayNode.AppendChild(child);
                    }
                    else
                    {
                        var arrayItem = new XmlNode(_database.GetString(prop.DataType));

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

    //verified same as scdatatools
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
                enumValues[i] = _database.GetString(_database.EnumOptions[enumDef.FirstValueIndex + i]);
            }

            result.Add(_database.GetString(enumDef.NameOffset), enumValues);
        }

        return result;
    }

    public void WriteTo(TextWriter writer, DataForgeStructDefinition structDef, ref SpanReader reader)
    {
        writer.Write('<');

        writer.Write(_database.GetString(structDef.NameOffset));

        var properties = structDef.EnumerateProperties(_database.StructDefinitions, _database.PropertyDefinitions);
        foreach (var property in properties.Where(a => a.IsAttribute))
        {
            //these properties are attributes
            writer.Write(' ');
            writer.Write(_database.GetString(property.NameOffset));
            writer.Write('=');
            writer.Write('"');
            switch (property.DataType)
            {
                case DataType.Boolean:
                    writer.Write(reader.ReadBoolean());
                    break;
                case DataType.Single:
                    writer.Write(reader.ReadSingle());
                    break;
                case DataType.Double:
                    writer.Write(reader.ReadDouble());
                    break;
                case DataType.Guid:
                    writer.Write(reader.Read<CigGuid>());
                    break;
                case DataType.SByte:
                    writer.Write(reader.ReadSByte());
                    break;
                case DataType.UInt16:
                    writer.Write(reader.ReadUInt16());
                    break;
                case DataType.UInt32:
                    writer.Write(reader.ReadUInt32());
                    break;
                case DataType.UInt64:
                    writer.Write(reader.ReadUInt64());
                    break;
                case DataType.Byte:
                    writer.Write(reader.ReadByte());
                    break;
                case DataType.Int16:
                    writer.Write(reader.ReadInt16());
                    break;
                case DataType.Int32:
                    writer.Write(reader.ReadInt32());
                    break;
                case DataType.Int64:
                    writer.Write(reader.ReadInt64());
                    break;
                case DataType.Reference:
                    writer.Write(reader.Read<DataForgeReference>());
                    break;
                case DataType.String:
                    writer.Write(_database.GetString(reader.Read<DataForgeStringId>()));
                    break;
                case DataType.Locale:
                    writer.Write(_database.GetString(reader.Read<DataForgeStringId>()));
                    break;
                case DataType.EnumChoice:
                    writer.Write(_database.GetString(reader.Read<DataForgeStringId>()));
                    break;
                case DataType.WeakPointer:
                    writer.Write(reader.Read<DataForgePointer>());
                    break;
                default:
                    throw new InvalidOperationException(nameof(DataType));
            }
        }

        writer.Write('>');

        foreach (var property in properties.Where(a => !a.IsAttribute))
        {
            var count = reader.ReadUInt32();
            var firstIndex = reader.ReadUInt32();

            writer.Write('<');
            writer.Write(_database.GetString(property.NameOffset));
            writer.Write(' ');
            writer.Write("__count__");
            writer.Write('=');
            writer.Write('"');
            writer.Write(count);
            writer.Write('"');
            writer.Write('>');

            for (var i = 0; i < count; i++)
            {
                var index = (int)firstIndex + i;

                if (property.DataType == DataType.Class)
                {
                    var structDef1 = _database.StructDefinitions[property.StructIndex];
                    var offset1 = _offsets[property.StructIndex][index];
                    var reader1 = _database.GetReader(offset1);

                    WriteTo(writer, structDef1, ref reader1);
                }
                else if (property.DataType is DataType.StrongPointer /*or DataType.WeakPointer*/)
                {
                    var reference = property.DataType switch
                    {
                        DataType.StrongPointer => _database.StrongValues[index],
                        DataType.WeakPointer => _database.WeakValues[index],
                        _ => throw new InvalidOperationException(nameof(DataType))
                    };

                    if (reference.StructIndex == 0xFFFFFFFF || reference.InstanceIndex == 0xFFFFFFFF) continue;

                    var structDef2 = _database.StructDefinitions[(int)reference.StructIndex];
                    var offset2 = _offsets[(int)reference.StructIndex][(int)reference.InstanceIndex];
                    var reader2 = _database.GetReader(offset2);

                    WriteTo(writer, structDef2, ref reader2);
                }
                else
                {
                    writer.Write('<');
                    writer.Write(_database.GetString(property.DataType));
                    writer.Write(' ');
                    writer.Write("__value");
                    writer.Write('=');
                    writer.Write('"');
                    switch (property.DataType)
                    {
                        case DataType.Byte:
                            writer.Write(reader.ReadByte());
                            break;
                        case DataType.Int16:
                            writer.Write(reader.ReadInt16());
                            break;
                        case DataType.Int32:
                            writer.Write(reader.ReadInt32());
                            break;
                        case DataType.Int64:
                            writer.Write(reader.ReadInt64());
                            break;
                        case DataType.SByte:
                            writer.Write(reader.ReadSByte());
                            break;
                        case DataType.UInt16:
                            writer.Write(reader.ReadUInt16());
                            break;
                        case DataType.UInt32:
                            writer.Write(reader.ReadUInt32());
                            break;
                        case DataType.UInt64:
                            writer.Write(reader.ReadUInt64());
                            break;
                        case DataType.Boolean:
                            writer.Write(reader.ReadBoolean());
                            break;
                        case DataType.Single:
                            writer.Write(reader.ReadSingle());
                            break;
                        case DataType.Double:
                            writer.Write(reader.ReadDouble());
                            break;
                        case DataType.Guid:
                            writer.Write(reader.Read<CigGuid>());
                            break;
                        case DataType.String:
                            writer.Write(_database.GetString(reader.Read<DataForgeStringId>()));
                            break;
                        case DataType.Locale:
                            writer.Write(_database.GetString(reader.Read<DataForgeStringId>()));
                            break;
                        case DataType.EnumChoice:
                            writer.Write(_database.GetString(reader.Read<DataForgeStringId>()));
                            break;
                        case DataType.Reference:
                            writer.Write(reader.Read<DataForgeReference>());
                            break;
                        case DataType.WeakPointer:
                            writer.Write(reader.Read<DataForgePointer>());
                            break;
                        default:
                            throw new InvalidOperationException(nameof(DataType));
                    }

                    writer.Write('/');

                    writer.Write('>');
                }
            }
        }
    }

    public void ExtractSingle2(string outputFolder, Regex? fileNameFilter = null, IProgress<double>? progress = null)
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
                var s = _database.GetString(record.FileNameOffset);
                if (!fileNameFilter.IsMatch(s))
                    continue;
            }

            var structDef = _database.StructDefinitions[record.StructIndex];
            var offset = _offsets[record.StructIndex][record.InstanceIndex];
            var reader = _database.GetReader(offset);

            WriteTo(writer, structDef, ref reader);

            ++progressValue;
            if (progressValue % 250 == 0 || progressValue == total)
                progress?.Report(progressValue / (double)total);
        }

        writer.WriteLine("</__root>");
    }

    public void X(string recordFileName, TextWriter writer)
    {
        var names = _database.RecordDefinitions.Select(a => _database.GetString(a.FileNameOffset)).Distinct().OrderBy(x => x.Length).ToArray();
        File.WriteAllLines("names.txt", names);

        var targetRecords = _database.RecordDefinitions.Where(a => _database.GetString(a.FileNameOffset) == recordFileName).ToArray();
        var xx = _database.RecordDefinitions.Single(x => x.Hash.ToString() == "66ee5bfc-d90b-41bd-ad2e-e0a2b3efe359");
        var x = Array.IndexOf(_database.RecordDefinitions, xx);
        writer.WriteLine("<__root>");

        foreach (var record in targetRecords)
        {
            var structDef = _database.StructDefinitions[record.StructIndex];
            var offset = _offsets[record.StructIndex][record.InstanceIndex];
            var reader = _database.GetReader(offset);
            var child = new XmlNode(_database.GetString(structDef.NameOffset));

            FillNode(child, structDef, ref reader);

            child.WriteTo(writer, 1);
        }

        writer.WriteLine("</__root>");
    }
}