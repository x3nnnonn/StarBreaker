using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{Name}")]
public sealed class ZipNode
{
    private static readonly Dictionary<int, ZipNode> _empty = new();

    public string Name { get; }
    public ZipEntry? ZipEntry { get; }
    public Dictionary<int, ZipNode> Children { get; }

    public ZipNode(string name)
    {
        Name = name;
        ZipEntry = null;
        Children = [];
    }

    public ZipNode(ZipEntry file, string name)
    {
        Name = name;
        ZipEntry = file;
        Children = _empty;
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
                current.Children[partHashCode] = new ZipNode(zipEntry, part.ToString());
                return;
            }

            if (!current.Children.TryGetValue(partHashCode, out var value))
            {
                value = new ZipNode(part.ToString());
                current.Children[partHashCode] = value;
            }

            current = value;
        }
    }
}