namespace StarBreaker.P4k;

public sealed class DiskFileSystem : IFileSystem
{
    public static readonly DiskFileSystem Instance = new DiskFileSystem();

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }
}