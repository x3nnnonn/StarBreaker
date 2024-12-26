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

    public XElement GetFromRecord(DataCoreRecord record)
    {
        return DataCore.GetFromPointer(record.StructIndex, record.InstanceIndex);
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
        var recordsByFileName = DataCore.GetRecordsByFileName(fileNameFilter);
        var total = recordsByFileName.Count;

        foreach (var (fileName, record) in recordsByFileName)
        {
            var filePath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var node = DataCore.GetFromPointer(record.StructIndex, record.InstanceIndex);

            node.Save(filePath);

            var currentProgress = Interlocked.Increment(ref progressValue);
            //only report progress every 250 records and when we are done
            if (currentProgress == total || currentProgress % 250 == 0)
                progress?.Report(currentProgress / (double)total);
        }
    }
}