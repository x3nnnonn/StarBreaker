namespace StarBreaker.FileSystem;

public sealed class RealFileSystem : IFileSystem
{
    public static readonly RealFileSystem Instance = new();

    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    public bool FileExists(string path) => File.Exists(path) || Directory.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
}