using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;

namespace StarBreaker.P4k;

public class Unp4ker : IDisposable
{
    private static readonly byte[] _key = [
        0x5E, 0x7A, 0x20, 0x02,
        0x30, 0x2E, 0xEB, 0x1A,
        0x3B, 0xB6, 0x17, 0xC3,
        0x0F, 0xDE, 0x1E, 0x47
    ];
    private readonly ZipFile zipFile;

    public Unp4ker(string p4kPath)
    {
        zipFile = new ZipFile(p4kPath);
        zipFile.KeysRequired += OnKeysRequired;
    }

    public void Extract(string outFolder, Regex? fileNameFilter = null, IProgress<double>? progress = null)
    {
        var numberOfEntries = zipFile.Count;
        var processedEntries = 0;
        
        foreach (var entry in zipFile)
        {
            if (fileNameFilter != null && !fileNameFilter.IsMatch(entry.Name))
            {
                processedEntries++;
                continue;
            }

            var path = Path.Combine(outFolder, entry.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var zipStream = zipFile.GetInputStream(entry);
            using var fileStream = File.Create(Path.Combine(outFolder, entry.Name));
            
            zipStream.CopyTo(fileStream);
            
            processedEntries++;
            progress?.Report((double) processedEntries / numberOfEntries);
        }
    }
    
    public IEnumerable<ZipEntry> Entries => zipFile;

    public void Dispose()
    {
        zipFile.KeysRequired -= OnKeysRequired;
        zipFile.Close();
        GC.SuppressFinalize(this);
    }
    
    private static void OnKeysRequired(object sender, KeysRequiredEventArgs e)
    {
        e.Key = _key;
    }
}
