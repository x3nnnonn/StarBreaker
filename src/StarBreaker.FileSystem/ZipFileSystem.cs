using System.IO.Compression;

namespace StarBreaker.FileSystem;

public sealed class ZipFileSystem : IFileSystem
{
    private readonly ZipArchive _archive;

    public ZipFileSystem(ZipArchive archive)
    {
        _archive = archive;
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return _archive.Entries
            .Where(entry => entry.FullName.StartsWith(path) && !entry.FullName.EndsWith('/'))
            .Select(entry => entry.FullName);
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        return _archive.Entries
            .Where(entry => entry.FullName.StartsWith(path) && entry.FullName.EndsWith('/'))
            .Select(entry => entry.FullName);
    }

    public bool FileExists(string path) => _archive.Entries.Any(entry => entry.FullName == path);

    public Stream OpenRead(string path)
    {
        var entry = _archive.GetEntry(path);
        
        if (entry == null)
            throw new FileNotFoundException("File not found in archive.", path);
        
        return entry.Open();
    }
}