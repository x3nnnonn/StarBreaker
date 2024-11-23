using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{Name}")]
public sealed class ZipNode
{
    private static readonly Dictionary<int, ZipNode> _empty = new();
    private readonly string _name;
    private readonly ZipEntry? _zipEntry;
    private readonly Dictionary<int, ZipNode> _children;

    public string Name => _name;
    public ZipEntry? ZipEntry => _zipEntry;
    public Dictionary<int, ZipNode> Children => _children;

    public ZipNode(string name)
    {
        _name = name;
        _zipEntry = null;
        _children = [];
    }

    public ZipNode(ZipEntry file, string name)
    {
        _name = name;
        _zipEntry = file;
        _children = _empty;
    }

    public void Insert(ZipEntry zipEntry)
    {
        var current = this;
        var name = zipEntry.Name.AsSpan();
        
        foreach (var range in name.Split('\\'))
        {
            var part = name[range];
            var partHashCode = string.GetHashCode(part);

            if (range.End.Value == name.Length)
            {
                // If this is the last part, we're at the file
                current._children[partHashCode] = new ZipNode(zipEntry, part.ToString());
                return;
            }

            if (!current._children.TryGetValue(partHashCode, out var value))
            {
                value = new ZipNode(part.ToString());
                current._children[partHashCode] = value;
            }

            current = value;
        }
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current._children.TryGetValue(string.GetHashCode(span[part]), out var value))
            {
                yield break;
            }

            current = value;
        }

        foreach (var child in current._children.Values.Where(x => x.ZipEntry == null))
        {
            yield return child.Name;
        }
    }

    public IEnumerable<string> GetFiles(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');

        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current._children.TryGetValue(string.GetHashCode(span[part]), out var value))
            {
                yield break;
            }

            current = value;
        }

        foreach (var child in current._children.Values.Where(x => x.ZipEntry != null))
        {
            yield return child.Name;
        }
    }
}