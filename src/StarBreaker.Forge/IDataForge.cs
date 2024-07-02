using System.Text.RegularExpressions;

namespace StarBreaker.Forge;

public interface IDataForge
{
    /// <summary>
    ///     Export all files in the DataForge to XML.
    /// </summary>
    /// <param name="outputFolder">Output folder</param>
    /// <param name="fileNameFilter">Regex to filter files to export</param>
    /// <param name="progress">Progress callback</param>
    void Extract(string outputFolder, Regex? fileNameFilter = null, IProgress<double>? progress = null);
    
    /// <summary>
    ///     Export all records in the DataCoreBinary into a single XML file.
    /// </summary>
    /// <param name="outputFolder">Output folder</param>
    /// <param name="fileNameFilter">Regex to filter files to export</param>
    /// <param name="progress">Progress callback</param>
    void ExtractSingle(string outputFolder, Regex? fileNameFilter = null, IProgress<double>? progress = null);
    
    /// <summary>
    ///     Export all enums in the DataForge to a dictionary.
    /// </summary>
    Dictionary<string, string[]> ExportEnums();
}