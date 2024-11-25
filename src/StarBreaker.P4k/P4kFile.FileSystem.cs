using StarBreaker.FileSystem;

namespace StarBreaker.P4k;

public sealed partial class P4kFile : IFileSystem
{
    public IEnumerable<string> GetDirectories(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.TryGetValue(string.GetHashCode(span[part]), out var value))
            {
                yield break;
            }

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
            if (!current.Children.TryGetValue(string.GetHashCode(span[part]), out var value))
            {
                yield break;
            }

            current = value;
        }

        foreach (var child in current.Children.Values.Where(x => x.ZipEntry != null))
        {
            yield return child.Name;
        }
    }

    public bool Exists(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = Root;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.TryGetValue(string.GetHashCode(span[part]), out var value))
            {
                return false;
            }

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
            if (!current.Children.TryGetValue(string.GetHashCode(span[part]), out var value))
            {
                throw new FileNotFoundException();
            }

            current = value;
        }

        if (current.ZipEntry == null)
        {
            throw new FileNotFoundException();
        }

        return Open(current.ZipEntry);
    }
}