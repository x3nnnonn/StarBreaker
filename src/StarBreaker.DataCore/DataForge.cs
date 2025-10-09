using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public static class DataForge
{
    public static DataForge<string> FromDcbPathJson(string path)
    {
        return new DataForge<string>(new DataCoreBinaryJson(new DataCoreDatabase(File.OpenRead(path))));
    }

    public static DataForge<string> FromDcbStreamJson(Stream dcbStream)
    {
        return new DataForge<string>(new DataCoreBinaryJson(new DataCoreDatabase(dcbStream)));
    }

    public static DataForge<string> FromDcbPathXml(string path)
    {
        return new DataForge<string>(new DataCoreBinaryXml(new DataCoreDatabase(File.OpenRead(path))));
    }

    public static DataForge<string> FromDcbStreamXml(Stream dcbStream)
    {
        return new DataForge<string>(new DataCoreBinaryXml(new DataCoreDatabase(dcbStream)));
    }

    public static DataForge<JsonObject> FromDcbPathJsonNode(string path)
    {
        return new DataForge<JsonObject>(new DataCoreBinaryJsonObject(new DataCoreDatabase(File.OpenRead(path))));
    }
}

public class DataForge<T>
{
    private readonly List<DataCoreTypeNode> _rootNodes;
    public IDataCoreBinary<T> DataCore { get; }


    public DataForge(IDataCoreBinary<T> dataCore)
    {
        DataCore = dataCore;
        _rootNodes = BuildTypeTree();
    }

    public Dictionary<string, DataCoreRecord> GetRecordsByFileName(string? fileNameFilter = null)
    {
        var structsPerFileName = new Dictionary<string, DataCoreRecord>();
        foreach (var recordId in DataCore.Database.MainRecords)
        {
            var record = DataCore.Database.GetRecord(recordId);
            var fileName = record.GetFileName(DataCore.Database);

            if (fileNameFilter != null && !FileSystemName.MatchesSimpleExpression(fileNameFilter, fileName))
                continue;

            structsPerFileName[fileName] = record;
        }

        return structsPerFileName;
    }

    public T GetFromRecord(DataCoreRecord record)
    {
        return DataCore.GetFromMainRecord(record);
    }

    public T GetFromRecord(CigGuid recordGuid)
    {
        return DataCore.GetFromMainRecord(DataCore.Database.GetRecord(recordGuid));
    }

    public Dictionary<string, string[]> ExportEnums()
    {
        var result = new Dictionary<string, string[]>(DataCore.Database.EnumDefinitions.Length);

        foreach (var enumDef in DataCore.Database.EnumDefinitions)
        {
            var enumValues = new string[enumDef.ValueCount];
            for (var i = 0; i < enumDef.ValueCount; i++)
            {
                enumValues[i] = DataCore.Database.GetString2(DataCore.Database.EnumOptions[enumDef.FirstValueIndex + i]);
            }

            result.Add(enumDef.GetName(DataCore.Database), enumValues);
        }

        return result;
    }

    public void ExtractAll(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        foreach (var (fileName, record) in recordsByFileName)
        {
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            DataCore.SaveRecordToFile(record, filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 1000 records and when we are done
            if (currentProgress == total || currentProgress % 1000 == 0)
                progress?.Report(currentProgress / (double)total);
        }

        progress?.Report(1);
    }

    public void ExtractAllParallel(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        Parallel.ForEach(recordsByFileName, kvp =>
        {
            var (fileName, record) = kvp;
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            DataCore.SaveRecordToFile(record, filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 1000 records and when we are done
            if (currentProgress == total || currentProgress % 1000 == 0)
                progress?.Report(currentProgress / (double)total);
        });

        progress?.Report(1);
    }

    public void ExtractTypesParallel(string outputFolder,  IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var total = DataCore.Database.StructDefinitions.Length;

        progress?.Report(progressValue);

        foreach (var rootNode in _rootNodes)
            ExtractTypeNode(rootNode, outputFolder, ref progressValue, progress);

        progress?.Report(total);
    }

    private void ExtractTypeNode(DataCoreTypeNode node, string currentDirectory, ref int progressValue, IProgress<double>? progress)
    {
        var structDef = node.StructDefinition;
        var nodeName = structDef.GetName(DataCore.Database);

        string filePath;
        if (node.Children.Count > 0)
        {
            var dirPath = Path.Combine(currentDirectory, nodeName);
            Directory.CreateDirectory(dirPath);

            // Save the node data to a file in this directory
            filePath = Path.Combine(dirPath, $"{nodeName}.xml");
        }
        else
        {
            filePath = Path.Combine(currentDirectory, $"{nodeName}.xml");
        }
        
        DataCore.SaveStructToFile(node.Index, filePath);

        progressValue++;
        progress?.Report(progressValue);

        foreach (var child in node.Children)
        {
            ExtractTypeNode(child, Path.Combine(currentDirectory, nodeName), ref progressValue, progress);
        }
    }

    public void ExtractEnumsParallel(string outputFolder, IProgress<double>? progress = null)
    {
        var progressValue = 0;
        var total = DataCore.Database.EnumDefinitions.Length;

        progress?.Report(progressValue);

        Parallel.For(0, DataCore.Database.EnumDefinitions.Length, i =>
        {
            var enumDef = DataCore.Database.EnumDefinitions[i];
            var enumName = enumDef.GetName(DataCore.Database);
            var filePath = Path.Combine(outputFolder, $"{enumName}.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            DataCore.SaveEnumToFile(i, filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 1000 records and when we are done
            if (currentProgress == total || currentProgress % 1000 == 0)
                progress?.Report(currentProgress / (double)total);
        });

        progress?.Report(1);
    }

    private List<DataCoreTypeNode> BuildTypeTree()
    {
        var definitions = DataCore.Database.StructDefinitions;

        var nodesMap = new Dictionary<int, DataCoreTypeNode>(definitions.Length);
        for (var i = 0; i < definitions.Length; i++)
        {
            nodesMap[i] = new DataCoreTypeNode(DataCore.Database, i);
        }

        var rootNodes = new List<DataCoreTypeNode>();
        foreach (var kvp in nodesMap)
        {
            var node = kvp.Value;
            var parentIndex = node.StructDefinition.ParentTypeIndex;

            if (parentIndex == -1)
            {
                rootNodes.Add(node);
            }
            else if (nodesMap.TryGetValue(parentIndex, out var parentNode))
            {
                parentNode.Children.Add(node);
            }
            else
            {
                throw new InvalidOperationException($"Parent type with index {parentIndex} not found.");
            }
        }

        return rootNodes;
    }
}