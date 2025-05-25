using System.IO.Compression;

namespace StarBreaker.SocPak;

public sealed class SocPakFile : ISocPakFile, IDisposable
{
    private readonly ZipArchive _zipArchive;
    private readonly FileStream _fileStream;
    
    public string SocPakPath { get; }
    public ZipArchiveEntry[] Entries { get; }
    public SocPakDirectoryNode Root { get; }

    private SocPakFile(string path, FileStream fileStream, ZipArchive zipArchive, ZipArchiveEntry[] entries, SocPakDirectoryNode root)
    {
        SocPakPath = path;
        _fileStream = fileStream;
        _zipArchive = zipArchive;
        Entries = entries;
        Root = root;
    }

    public static SocPakFile FromFile(string filePath, IProgress<double>? progress = null)
    {
        progress?.Report(0);
        
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, false);
        
        var entries = zipArchive.Entries.ToArray();
        var root = new SocPakDirectoryNode("Root");
        
        // Build the directory tree
        var reportInterval = Math.Max(entries.Length / 50, 1);
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            InsertEntry(root, entry);
            
            if (i % reportInterval == 0)
                progress?.Report(i / (double)entries.Length);
        }
        
        progress?.Report(1);
        return new SocPakFile(filePath, fileStream, zipArchive, entries, root);
    }

    private static void InsertEntry(SocPakDirectoryNode root, ZipArchiveEntry entry)
    {
        if (string.IsNullOrEmpty(entry.FullName))
            return;

        var pathParts = entry.FullName.Split('/', '\\');
        var currentDir = root;

        // Navigate/create directory structure
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            if (!string.IsNullOrEmpty(pathParts[i]))
            {
                currentDir = currentDir.GetOrCreateChild(pathParts[i]);
            }
        }

        // Add the file to the final directory
        var fileName = pathParts[^1];
        if (!string.IsNullOrEmpty(fileName))
        {
            var fileNode = new SocPakFileNode(entry, currentDir);
            currentDir.AddFileChild(fileNode);
        }
    }

    public Stream OpenStream(ZipArchiveEntry entry)
    {
        return entry.Open();
    }

    public void Dispose()
    {
        _zipArchive?.Dispose();
        _fileStream?.Dispose();
    }
} 