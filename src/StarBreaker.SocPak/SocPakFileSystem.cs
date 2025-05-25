using System.IO.Compression;
using StarBreaker.FileSystem;

namespace StarBreaker.SocPak;

public sealed class SocPakFileSystem : IFileSystem
{
    private readonly SocPakFile _socPakFile;

    public SocPakFileSystem(SocPakFile socPakFile)
    {
        _socPakFile = socPakFile ?? throw new ArgumentNullException(nameof(socPakFile));
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        var directory = FindDirectoryNode(path);
        if (directory == null)
            yield break;

        foreach (var file in directory.GetFiles())
        {
            yield return file.FullPath;
        }
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
    {
        var directory = FindDirectoryNode(path);
        if (directory == null)
            yield break;

        foreach (var file in directory.GetFiles())
        {
            if (MatchesPattern(file.Name, searchPattern))
            {
                yield return file.FullPath;
            }
        }
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        var directory = FindDirectoryNode(path);
        if (directory == null)
            yield break;

        foreach (var subDir in directory.GetDirectories())
        {
            yield return subDir.Name;
        }
    }

    public bool FileExists(string path)
    {
        return FindFileEntry(path) != null;
    }

    public Stream OpenRead(string path)
    {
        var entry = FindFileEntry(path);
        if (entry == null)
            throw new FileNotFoundException($"File not found: {path}");

        return _socPakFile.OpenStream(entry);
    }

    public byte[] ReadAllBytes(string path)
    {
        using var stream = OpenRead(path);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private ZipArchiveEntry? FindFileEntry(string path)
    {
        var normalizedPath = NormalizePath(path);
        return _socPakFile.Entries.FirstOrDefault(e => 
            string.Equals(NormalizePath(e.FullName), normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    private SocPakDirectoryNode? FindDirectoryNode(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/" || path == "\\")
            return _socPakFile.Root;

        var pathParts = NormalizePath(path).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentDir = _socPakFile.Root;

        foreach (var part in pathParts)
        {
            if (currentDir.Children.TryGetValue(part, out var child) && child is SocPakDirectoryNode childDir)
            {
                currentDir = childDir;
            }
            else
            {
                return null;
            }
        }

        return currentDir;
    }



    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static bool MatchesPattern(string name, string pattern)
    {
        if (pattern == "*")
            return true;

        // Simple wildcard matching - could be enhanced with proper regex
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern.Replace("*", ".*").Replace("?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(name, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
    }
} 