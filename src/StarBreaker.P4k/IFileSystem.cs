using System.IO.Enumeration;

namespace StarBreaker.P4k;

public interface IFileSystem
{
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
    Stream OpenRead(string path);
}