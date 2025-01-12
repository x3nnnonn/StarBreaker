using System.Globalization;
using System.IO.Enumeration;
using System.Text;
using System.Xml.Linq;

namespace StarBreaker.P4k;

public class P4kExtractOptions
{
    public required bool ProcessCryXml { get; init; }
    public required bool ProcessSocpaks { get; init; }
    public required bool ProcessDdsTextures { get; init; }

    //TODO: models (cgf)
    //TODO: sounds (wwise)
    //TODO: videos (bik)
    //TODO: ???
}

public sealed class P4kExtractor
{
    private readonly P4kFile _p4KFile;

    public P4kExtractor(P4kFile p4KFile)
    {
        _p4KFile = p4KFile;
    }

    public void Extract(string outputDir, string? filter = null, IProgress<double>? progress = null)
    {
        //TODO: if the filter is for *.dds, make sure to include *.dds.N too. Maybe do the pre processing before we filter?
        var filteredEntries = (filter is null
            ? _p4KFile.Entries.ToArray()
            : _p4KFile.Entries.Where(entry => FileSystemName.MatchesSimpleExpression(filter, entry.Name))).ToArray();

        var numberOfEntries = filteredEntries.Length;
        var fivePercent = numberOfEntries / 20;
        var processedEntries = 0;

        progress?.Report(0);

        var lockObject = new Lock();

        //TODO: Preprocessing step:
        // 1. start with the list of total files
        // 2. run the following according to the filter:
        // 3. find one-shot single file procesors
        // 4. find file -> multiple file processors
        // 5. find multiple file -> single file unsplit processors - remove from the list so we don't double process
        // run it!
        Parallel.ForEach(filteredEntries,
            entry =>
            {
                if (entry.UncompressedSize == 0)
                    return;

                var entryPath = Path.Combine(outputDir, entry.Name);
                //if (File.Exists(entryPath))
                //    return;

                Directory.CreateDirectory(Path.GetDirectoryName(entryPath) ?? throw new InvalidOperationException());
                using (var writeStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                           bufferSize: entry.UncompressedSize > int.MaxValue ? 81920 : (int)entry.UncompressedSize, useAsync: true))
                {
                    using (var entryStream = _p4KFile.OpenStream(entry))
                    {
                        entryStream.CopyTo(writeStream);
                    }
                }

                Interlocked.Increment(ref processedEntries);
                if (processedEntries == numberOfEntries || processedEntries % fivePercent == 0)
                {
                    using (lockObject.EnterScope())
                    {
                        progress?.Report(processedEntries / (double)numberOfEntries);
                    }
                }
            }
        );

        progress?.Report(1);
    }

    public void ExtractDummies(string outputDir)
    {
        WriteFileForNode(outputDir, _p4KFile.Root);
    }

    private static void WriteFileForNode(string baseDir, ZipNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Name) || node.ZipEntry != null)
            throw new InvalidOperationException("Node name is not a directory");

        var dir = new XElement("Directory",
            new XAttribute("Name", node.Name)
        );

        foreach (var (_, childNode) in node.Children.OrderBy(x => x.Key))
        {
            if (childNode.ZipEntry == null)
            {
                if (string.IsNullOrWhiteSpace(childNode.Name))
                    throw new InvalidOperationException("Node name is not a directory");

                //if we're a directory, Call ourselves recursively
                WriteFileForNode(Path.Combine(baseDir, childNode.Name), childNode);
            }
            else
            {
                dir.Add(new XElement("File",
                    new XAttribute("Name", Path.GetFileName(childNode.ZipEntry.Name)),
                    new XAttribute("CRC32", $"0x{childNode.ZipEntry.Crc32:X8}"),
                    //Revisit: they seem to change lastmodified a lot while the crc32 stays the same. I'll just ignore the date for now.
                    // new XAttribute("LastModified", childNode.ZipEntry.LastModified.ToString("O")),
                    new XAttribute("Size", childNode.ZipEntry.UncompressedSize.ToString(CultureInfo.InvariantCulture)),
                    //new XAttribute("CompressedSize", childNode.ZipEntry.CompressedSize.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("CompressionType", childNode.ZipEntry.CompressionMethod.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("Encrypted", childNode.ZipEntry.IsCrypted.ToString(CultureInfo.InvariantCulture))
                ));
            }
        }

        if (dir.HasElements)
        {
            var filePath = Path.Combine(baseDir, node.Name) + ".xml";
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            dir.Save(filePath);
        }
    }
}