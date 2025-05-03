using System.IO.Enumeration;
using StarBreaker.Common;
using StarBreaker.FileSystem;

namespace StarBreaker.P4k;

public class P4kFileSystem : IFileSystem
{
    public IP4kFile P4kFile { get; }
    public P4kDirectoryNode Root { get; }

    public P4kFileSystem(IP4kFile p4kFile)
    {
        P4kFile = p4kFile;
        Root = p4kFile.Root;
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                yield break;

            if (value is not P4kDirectoryNode directory)
                yield break;

            current = directory;
        }

        foreach (var child in current.Children.Values.OfType<P4kDirectoryNode>())
        {
            yield return child.Name;
        }
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');

        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                yield break;

            if (value is not P4kDirectoryNode directory)
                yield break;

            current = directory;
        }

        foreach (var child in current.Children.Values.OfType<P4kFileNode>())
        {
            yield return child.ZipEntry.Name;
        }
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');

        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                yield break;

            if (value is not P4kDirectoryNode directory)
                yield break;

            current = directory;
        }

        foreach (var child in current.Children.Values.OfType<P4kFileNode>())
        {
            if (!FileSystemName.MatchesSimpleExpression(searchPattern, child.ZipEntry.Name.Split('\\').Last()))
                continue;

            yield return child.ZipEntry.Name;

        }
    }

    public bool FileExists(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                return false;

            if (value is P4kDirectoryNode directory)
                current = directory;
            else
                return value is P4kFileNode && index == partsCount - 1;
        }

        return false;
    }

    public Stream OpenRead(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                throw new FileNotFoundException();

            if (value is P4kDirectoryNode directory)
                current = directory;
            else if (value is P4kFileNode file && index == partsCount - 1)
                return P4kFile.OpenStream(file.ZipEntry);
            else
                throw new FileNotFoundException();
        }

        throw new FileNotFoundException();
    }

    public byte[] ReadAllBytes(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                throw new FileNotFoundException();

            if (value is P4kDirectoryNode directory)
                current = directory;
            else if (value is P4kFileNode file && index == partsCount - 1)
                return P4kFile.OpenStream(file.ZipEntry).ToArray();
            else
                throw new FileNotFoundException();
        }

        throw new FileNotFoundException();
    }
}