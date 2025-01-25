using System.IO.Enumeration;
using System.Xml.Linq;

namespace StarBreaker.DataCore;

public class DataForge
{
    public DataCoreBinary DataCore { get; }

    public DataForge(Stream stream)
    {
        DataCore = new DataCoreBinary(new DataCoreDatabase(stream));
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

    public XElement GetFromRecord(DataCoreRecord record, DataCoreExtractionOptions? options = null)
    {
        var context = new DataCoreExtractionContext(record.GetFileName(DataCore.Database), options ?? GetDefaultExtractionOptions());
        return DataCore.GetFromMainRecord(record, context);
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

    public void ExtractAll(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null, DataCoreExtractionOptions? options = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        foreach (var (fileName, record) in recordsByFileName)
        {
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var node = GetFromRecord(record, options);

            node.Save(filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 250 records and when we are done
            if (currentProgress == total || currentProgress % 250 == 0)
                progress?.Report(currentProgress / (double)total);
        }

        progress?.Report(1);
    }

    public void ExtractAllParallel(string outputFolder, string? fileNameFilter = null, IProgress<double>? progress = null, DataCoreExtractionOptions? options = null)
    {
        var progressValue = 0;
        var recordsByFileName = GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        Parallel.ForEach(recordsByFileName, kvp =>
        {
            var (fileName, record) = kvp;
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var node = GetFromRecord(record, options);

            node.Save(filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 250 records and when we are done
            if (currentProgress == total || currentProgress % 250 == 0)
                progress?.Report(currentProgress / (double)total);
        });

        progress?.Report(1);
    }

    public void ExtractUnp4k(string outputFileName, IProgress<double>? progress = null, DataCoreExtractionOptions? options = null)
    {
        var progressValue = 0;
        var total = DataCore.Database.MainRecords.Count;

        var doc = new XDocument(new XElement("DataCore"));

        foreach (var recordId in DataCore.Database.MainRecords)
        {
            var record = DataCore.Database.GetRecord(recordId);
            var context = new DataCoreExtractionContext(record.GetFileName(DataCore.Database), options ?? GetDefaultExtractionOptions());
            var node = DataCore.GetFromMainRecord(record, context);

            doc.Root?.Add(node);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 250 records and when we are done
            if (currentProgress == total || currentProgress % 250 == 0)
                progress?.Report(currentProgress / (double)total);
        }

        doc.Save(outputFileName);
    }

    private static DataCoreExtractionOptions GetDefaultExtractionOptions() => new()
    {
        ShouldWriteMetadata = false,
        ShouldWriteTypeNames = true,
        ShouldWriteBaseTypeNames = false,
        ShouldWriteEnumMetadata = false,
        ShouldSkipEmptyArrays = true
    };
}
