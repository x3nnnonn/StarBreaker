using System.IO.Compression;

namespace StarBreaker.P4k;

public sealed class ZipFileProcessor : IFileProcessor
{
    public bool CanProcess(string entryName, Stream stream)
    {
        //todo: optimistic list of extensions?
        return entryName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
    }

    public void ProcessEntry(string outputRootFolder, string entryName, Stream stream)
    {
        var entryPath = Path.Combine(outputRootFolder, Path.ChangeExtension(entryName, "unzipped"));
        
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var childEntry in archive.Entries)
        {
            if (childEntry.Length == 0)
                continue;
            
            using var childStream = childEntry.Open();

            var processor = FileProcessors.GetProcessor(childEntry.FullName, childStream);
            processor.ProcessEntry(entryPath, childEntry.FullName, childStream);
        }
    }
}