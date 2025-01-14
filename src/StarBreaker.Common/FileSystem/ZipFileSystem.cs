using System.IO.Compression;
using System.IO.Enumeration;

namespace StarBreaker.FileSystem;

public sealed class ZipFileSystem : IFileSystem
{
  private readonly ZipArchive _archive;

  public ZipFileSystem(ZipArchive archive)
  {
    _archive = archive;
  }

  public IEnumerable<string> EnumerateFiles(string path)
  {
    return _archive.Entries
        .Where(entry => entry.FullName.StartsWith(path) && !entry.FullName.EndsWith('/'))
        .Select(entry => entry.FullName);
  }

  public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
  {
    return _archive.Entries
        .Where(entry => entry.FullName.StartsWith(path) && !entry.FullName.EndsWith('/') && FileSystemName.MatchesSimpleExpression(entry.Name, searchPattern))
        .Select(entry => entry.FullName);
  }

  public IEnumerable<string> EnumerateDirectories(string path)
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

  public byte[] ReadAllBytes(string path)
  {
    var entry = _archive.GetEntry(path);

    if (entry == null)
      throw new FileNotFoundException("File not found in archive.", path);

    using var stream = entry.Open();
    using var memoryStream = new MemoryStream();
    stream.CopyTo(memoryStream);
    return memoryStream.ToArray();
  }
}