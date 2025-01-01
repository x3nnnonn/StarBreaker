using System.IO.Enumeration;

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
                if (File.Exists(entryPath))
                    return;

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
}