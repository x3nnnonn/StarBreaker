namespace StarBreaker.Extraction;

public sealed class P4kProcessorExtractor
{
    private readonly P4kDirectoryNode _root;

    public P4kProcessorExtractor(P4kDirectoryNode root)
    {
        _root = root;
    }

    public void Extract(string baseDir, IProgress<double>? progress = null)
    {
        var entries = _root.CollectAllFiles().ToArray();

        var numberOfEntries = entries.Length;
        var fivePercent = Math.Max(numberOfEntries / 20, 1);
        var processedEntries = 0;

        progress?.Report(0);

        var lockObject = new Lock();

        Parallel.ForEach(entries, fileNode =>
        {
            ExtractEntry(baseDir, fileNode);

            var newIncrement = Interlocked.Increment(ref processedEntries);
            if (newIncrement == numberOfEntries || newIncrement % fivePercent == 0)
            {
                using (lockObject.EnterScope())
                {
                    progress?.Report(newIncrement / (double)numberOfEntries);
                }
            }
        });

        progress?.Report(1);
    }

    private static void ExtractEntry(string baseDir, IP4kFileNode fileNode)
    {
        using var stream = fileNode.Open();
        var outputPath = Path.Combine(baseDir, fileNode.RelativeOutputPath);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (outputDir is null)
            throw new InvalidOperationException("Could not determine output directory.");

        Directory.CreateDirectory(outputDir);
        using var fileStream = File.Create(outputPath);
        stream.CopyTo(fileStream);
    }
}