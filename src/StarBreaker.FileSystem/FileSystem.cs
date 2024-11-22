namespace StarBreaker.FileSystem;

public sealed class FileSystem : IFileSystem
{
    public static readonly FileSystem Instance = new();
    
    public IEnumerable<string> GetFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> GetDirectories(string path) => Directory.EnumerateDirectories(path);

    public bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);
}