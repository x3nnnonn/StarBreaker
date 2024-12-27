using System.IO.Enumeration;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace StarBreaker.DataCore;

public class DataForge
{
    public DataCoreBinary DataCore { get; }

    public DataForge(Stream stream)
    {
        DataCore = new DataCoreBinary(stream);
    }

    public Dictionary<string, DataCoreRecord> GetRecordsByFileName(string? fileNameFilter = null)
    {
        var structsPerFileName = new Dictionary<string, DataCoreRecord>();
        foreach (var record in DataCore.Database.RecordDefinitions)
        {
            var fileName = record.GetFileName(DataCore.Database);

            if (fileNameFilter != null && !FileSystemName.MatchesSimpleExpression(fileNameFilter, fileName))
                continue;

            //this looks a lil wonky, but it's correct.
            //we will either find only on record for any given name,
            //or when we find multiple, we only care about the last one.
            structsPerFileName[fileName] = record;
        }

        return structsPerFileName;
    }

    public XElement GetFromRecord(DataCoreRecord record)
    {
        return DataCore.GetFromRecord(record);
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

            var node = DataCore.GetFromRecord(record);

            node.Save(filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 250 records and when we are done
            if (currentProgress == total || currentProgress % 250 == 0)
                progress?.Report(currentProgress / (double)total);
        }
    }
}