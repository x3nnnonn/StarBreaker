using StarBreaker.FileSystem;

namespace StarBreaker.P4k;

public class P4kFileSystem : IFileSystem
{
    public IP4kFile P4kFile { get; }
    public ZipNode Root { get; }

    public P4kFileSystem(IP4kFile p4kFile)
    {
        P4kFile = p4kFile;
        Root = p4kFile.Root;
    }

    public IEnumerable<string> GetDirectories(string path)
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

            current = value;
        }

        foreach (var child in current.Children.Values.Where(x => x.ZipEntry == null))
        {
            yield return child.Name;
        }
    }

    public IEnumerable<string> GetFiles(string path)
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

            current = value;
        }

        foreach (var child in current.Children.Values.Where(x => x.ZipEntry != null))
        {
            yield return child.Name;
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

            current = value;
        }

        return current.ZipEntry != null;
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

            current = value;
        }

        if (current.ZipEntry == null)
            throw new FileNotFoundException();

        //Is this a bad idea? Most things that use this rely on the stream being seekable.
        return new MemoryStream(P4kFile.OpenInMemory(current.ZipEntry));
    }
}