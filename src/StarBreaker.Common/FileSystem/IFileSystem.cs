namespace StarBreaker.FileSystem;

public interface IFileSystem
{
    IEnumerable<string> EnumerateFiles(string path);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern);

    IEnumerable<string> EnumerateDirectories(string path);

    bool FileExists(string path);

    Stream OpenRead(string path);

    byte[] ReadAllBytes(string path);
}