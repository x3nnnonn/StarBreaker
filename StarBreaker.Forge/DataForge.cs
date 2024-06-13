using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace StarBreaker.Forge;

public sealed class DataForge : IDataForge
{
    private readonly string _outputFolder;
    public readonly Database _database;
    private readonly Dictionary<int, int[]> _offsets;
    private int _progress;

    public DataForge(byte[] allBytes, string outputFolder)
    {
        _outputFolder = outputFolder;
        _database = new Database(allBytes, out var bytesRead);
        _offsets = ReadOffsets(bytesRead);

        // Dictionary<CigGuid, int> dd = new();
        // for (var index = 0; index < _database.RecordDefinitions.Span.Length; index++)
        // {
        //     var record = _database.RecordDefinitions.Span[index];
        //     dd.Add(record.Hash, index);
        // }
        //
        // var records = _database.RecordDefinitions.Span;
        //
        // foreach (var reference in _database.ReferenceValues.Span)
        // {
        //     if (reference.Item1 == 0xFFFFFFFF || reference.Value == CigGuid.Empty) continue;
        //     
        //     var record = records[dd[reference.Value]];
        //     var offset = _offsets[record.StructIndex][reference.Item1];
        //     Console.WriteLine($"{offset} | {record} ");
        // }
    }

    public void Export(Regex? fileNameFilter = null, IProgress<double>? progress = null)
    {
        _progress = 0;
        var structsPerFileName = new Dictionary<string, List<DataForgeRecord>>();
        foreach (ref readonly var record in _database.RecordDefinitions.Span)
        {
            var s = _database.GetString(record.FileNameOffset);

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
            var structs = _database.StructDefinitions.Span;
            var filePath = Path.Combine(_outputFolder, data.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            if (data.Value.Count == 1)
            {
                using var writer = new StreamWriter(filePath);

                var record = data.Value[0]; 
                var structDef = structs[record.StructIndex];
                var offset = _offsets[record.StructIndex][record.InstanceIndex];
                
                var reader = new ArrayReader(_database.Bytes, offset);
                
                var node = new XmlNode(structDef.NameOffset);
                
                FillNode(node, structDef, reader, 0);
                
                node.WriteTo(writer, 0, _database, _offsets);
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

                    var reader = new ArrayReader(_database.Bytes, offset);

                    var node = new XmlNode(structDef.NameOffset);

                    FillNode(node, structDef, reader, 0);

                    node.WriteTo(writer, 1, _database, _offsets);
                }
            
                writer.WriteLine();
                writer.Write("</");
                writer.Write("__root");
                writer.Write('>');
            }
            var currentProgress = Interlocked.Increment(ref _progress);
            //only report progress every 250 records and when we are done
            if (currentProgress == total || currentProgress % 250 == 0)
                progress?.Report(currentProgress / (double)total);
        });
    }

    public void ExportSingle(Regex? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var total = _database.RecordDefinitions.Length;
        using var writer = new StreamWriter(Path.Combine(_outputFolder, "StarBreaker.Export.xml"), false, Encoding.UTF8, 1024 * 1024);
        writer.WriteLine("<__root>");

        var structs = _database.StructDefinitions.Span;
        var records = _database.RecordDefinitions.Span;

        foreach (ref readonly var record in records)
        {
            if (fileNameFilter != null)
            {
                var s = _database.GetString(record.FileNameOffset);
                if (!fileNameFilter.IsMatch(s))
                    continue;
            }
            
            var structDef = structs[record.StructIndex];
            var offset = _offsets[record.StructIndex][record.InstanceIndex];
            var reader = new ArrayReader(_database.Bytes, offset);
            var child = new XmlNode(structDef.NameOffset);

            FillNode(child, structDef, reader, 0);

            child.WriteTo(writer, 1, _database, _offsets);
            
            ++progressValue;
            if (progressValue % 250 == 0 || progressValue == total)
                progress?.Report(progressValue / (double)total);
        }
        
        writer.WriteLine("</__root>");
    }

    private void FillNode(XmlNode node, DataForgeStructDefinition structDef, ArrayReader reader, int depth)
    {
        if (depth > 100)
        {
            var parents = new List<XmlNode>();
            var parent = node;
            while (parent._parent != null)
            {
                parents.Add(parent);
                parent = parent._parent;
            }
            throw new InvalidOperationException("Depth limit reached");
        }
        
        var structs = _database.StructDefinitions.Span;

        foreach (ref readonly var prop in structDef.EnumerateProperties(_database))
        {
            if (prop.ConversionType == ConversionType.Attribute)
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement switch here looks kinda ugly idk
                if (prop.DataType == DataType.Class)
                {
                    var structDef3 = structs[prop.StructIndex];

                    var childClass = new XmlNode(prop.NameOffset);

                    node.AppendChild(childClass);
                    
                    FillNode(childClass, structDef3, reader, depth + 1);
                }
                else if (prop.DataType is DataType.StrongPointer/* or DataType.WeakPointer*/)
                {
                    var ptr = reader.Read<DataForgePointer>();
                
                    if (ptr.StructIndex == 0xFFFFFFFF || ptr.InstanceIndex == 0xFFFFFFFF) continue;
                
                    var structDef2 = structs[(int)ptr.StructIndex];
                    var offset2 = _offsets[(int)ptr.StructIndex][(int)ptr.InstanceIndex];
                
                    var reader2 = new ArrayReader(_database.Bytes, offset2);
                
                    var child = new XmlNode(prop.NameOffset);
                
                    node.AppendChild(child);
                    
                    FillNode(child, structDef2, reader2, depth + 1);
                }
                else
                {
                    node.AppendAttribute(prop.DataType switch
                    {
                        DataType.Boolean => new XmlAttribute<bool>(prop.NameOffset, reader.Read<bool>(), _database),
                        DataType.Single => new XmlAttribute<float>(prop.NameOffset, reader.Read<float>(), _database),
                        DataType.Double => new XmlAttribute<double>(prop.NameOffset, reader.Read<double>(), _database),
                        DataType.Guid => new XmlAttribute<CigGuid>(prop.NameOffset, reader.Read<CigGuid>(), _database),
                        DataType.SByte => new XmlAttribute<sbyte>(prop.NameOffset, reader.Read<sbyte>(), _database),
                        DataType.UInt16 => new XmlAttribute<ushort>(prop.NameOffset, reader.Read<ushort>(), _database),
                        DataType.UInt32 => new XmlAttribute<uint>(prop.NameOffset, reader.Read<uint>(), _database),
                        DataType.UInt64 => new XmlAttribute<ulong>(prop.NameOffset, reader.Read<ulong>(), _database),
                        DataType.Byte => new XmlAttribute<byte>(prop.NameOffset, reader.Read<byte>(), _database),
                        DataType.Int16 => new XmlAttribute<short>(prop.NameOffset, reader.Read<short>(), _database),
                        DataType.Int32 => new XmlAttribute<int>(prop.NameOffset, reader.Read<int>(), _database),
                        DataType.Int64 => new XmlAttribute<long>(prop.NameOffset, reader.Read<long>(), _database),
                        DataType.Reference => new XmlAttribute<DataForgeReference>(prop.NameOffset, reader.Read<DataForgeReference>(), _database),
                        DataType.String => new XmlAttribute<DataForgeStringId>(prop.NameOffset, reader.Read<DataForgeStringId>(), _database),
                        DataType.Locale => new XmlAttribute<DataForgeStringId>(prop.NameOffset, reader.Read<DataForgeStringId>(), _database),
                        DataType.EnumChoice => new XmlAttribute<DataForgeStringId>(prop.NameOffset, reader.Read<DataForgeStringId>(), _database),
                        //TODO: remove from here when we implement pointers
                        DataType.WeakPointer or DataType.StrongPointer => new XmlAttribute<DataForgePointer>(prop.NameOffset, reader.Read<DataForgePointer>(), _database),
                        DataType.Class => throw new UnreachableException(),
                        _ => throw new NotImplementedException()
                    });
                }
            }
            else
            {
                var count = reader.Read<uint>();
                var firstIndex = reader.Read<uint>();

                var arrayNode = new XmlNode(prop.NameOffset);
                var dataTypeNameStringKey = _database.GetDataTypeStringId(prop.DataType);
                var valueStringKey = _database.ValueStringId;
                node.AppendChild(arrayNode);

                for (var i = 0; i < count; i++)
                {
                    var index = (int)firstIndex + i;

                    if (prop.DataType == DataType.Class)
                    {
                        var structDef1 = structs[prop.StructIndex];
                        var offset1 = _offsets[prop.StructIndex][index];
                        var reader1 = new ArrayReader(_database.Bytes, offset1);

                        var child = new XmlNode(structDef1.NameOffset);
                        
                        arrayNode.AppendChild(child);

                        FillNode(child, structDef1, reader1, depth + 1);
                    }
                    else if (prop.DataType is DataType.StrongPointer /*or DataType.WeakPointer*/)
                    {
                        var reference = prop.DataType switch
                        {
                            DataType.StrongPointer => _database.StrongValues.Span[index],
                            DataType.WeakPointer => _database.WeakValues.Span[index],
                            _ => throw new InvalidOperationException(nameof(DataType))
                        };
                        
                        if (reference.StructIndex == 0xFFFFFFFF || reference.InstanceIndex == 0xFFFFFFFF) continue;
                        
                        var structDef2 = structs[(int)reference.StructIndex];
                        var offset2 = _offsets[(int)reference.StructIndex][(int)reference.InstanceIndex];
                        var reader2 = new ArrayReader(_database.Bytes, offset2);
                        
                        var child = new XmlNode(prop.NameOffset);
                        
                        arrayNode.AppendChild(child);
                        
                        FillNode(child, structDef2, reader2, depth + 1);
                    }
                    else
                    {
                        var arrayItem = new XmlNode(dataTypeNameStringKey);
                        arrayNode.AppendChild(arrayItem);

                        arrayItem.AppendAttribute(prop.DataType switch
                        {
                            DataType.Byte => new XmlAttribute<byte>(valueStringKey, _database.UInt8Values.Span[index], _database),
                            DataType.Int16 => new XmlAttribute<short>(valueStringKey, _database.Int16Values.Span[index], _database),
                            DataType.Int32 => new XmlAttribute<int>(valueStringKey, _database.Int32Values.Span[index], _database),
                            DataType.Int64 => new XmlAttribute<long>(valueStringKey, _database.Int64Values.Span[index], _database),
                            DataType.SByte => new XmlAttribute<sbyte>(valueStringKey, _database.Int8Values.Span[index], _database),
                            DataType.UInt16 => new XmlAttribute<ushort>(valueStringKey, _database.UInt16Values.Span[index], _database),
                            DataType.UInt32 => new XmlAttribute<uint>(valueStringKey, _database.UInt32Values.Span[index], _database),
                            DataType.UInt64 => new XmlAttribute<ulong>(valueStringKey, _database.UInt64Values.Span[index], _database),
                            DataType.Boolean => new XmlAttribute<bool>(valueStringKey, _database.BooleanValues.Span[index], _database),
                            DataType.Single => new XmlAttribute<float>(valueStringKey, _database.SingleValues.Span[index], _database),
                            DataType.Double => new XmlAttribute<double>(valueStringKey, _database.DoubleValues.Span[index], _database),
                            DataType.Guid => new XmlAttribute<CigGuid>(valueStringKey, _database.GuidValues.Span[index], _database),
                            DataType.String => new XmlAttribute<DataForgeStringId>(valueStringKey, _database.StringIdValues.Span[index], _database),
                            DataType.Locale => new XmlAttribute<DataForgeStringId>(valueStringKey, _database.LocaleValues.Span[index], _database),
                            DataType.EnumChoice => new XmlAttribute<DataForgeStringId>(valueStringKey, _database.EnumValues.Span[index], _database),
                            DataType.Reference => new XmlAttribute<DataForgeReference>(valueStringKey, _database.ReferenceValues.Span[index], _database),
                            //TODO: remove from here when we implement pointers
                            DataType.StrongPointer or DataType.WeakPointer => new XmlAttribute<DataForgePointer>(valueStringKey, _database.StrongValues.Span[index], _database),
                            DataType.Class => throw new UnreachableException(),
                            _ => throw new InvalidOperationException(nameof(DataType))
                        });
                    }
                }

                arrayNode.AppendAttribute(new XmlAttribute<uint>(_database.CountStringId, count, _database));
            }
        }
    }

    //verified same as scdatatools
    private Dictionary<int, int[]> ReadOffsets(int initialOffset)
    {
        var dataMappings = _database.DataMappings.Span;
        var structDefinitions = _database.StructDefinitions.Span;
        var instances = new Dictionary<int, int[]>();

        foreach (ref readonly var mapping in dataMappings)
        {
            var arr = new int[mapping.StructCount];
            var structDef = structDefinitions[mapping.StructIndex];
            var structSize = structDef.CalculateSize(_database, structDefinitions);

            for (var i = 0; i < mapping.StructCount; i++)
            {
                arr[i] = initialOffset;
                initialOffset += structSize;
            }

            instances.Add(mapping.StructIndex, arr);
        }

        Debug.Assert(initialOffset == _database.Bytes.Length);

        return instances;
    }

    public Dictionary<string, string[]> ExportEnums()
    {
        var enumDefinitions = _database.EnumDefinitions.Span;
        var enumOptions = _database.EnumOptions.Span;

        var result = new Dictionary<string, string[]>(enumDefinitions.Length);
        foreach (ref readonly var enumDef in enumDefinitions)
        {
            var enumValues = new string[enumDef.ValueCount];
            for (var i = 0; i < enumDef.ValueCount; i++)
            {
                enumValues[i] = _database.GetString(enumOptions[enumDef.FirstValueIndex + i]);
            }

            result.Add(_database.GetString(enumDef.NameOffset), enumValues);
        }

        return result;
    }
}