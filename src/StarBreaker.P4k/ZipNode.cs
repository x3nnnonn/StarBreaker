using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[DebuggerDisplay("{DebugName}")]
public sealed class ZipNode
{
    private static readonly Dictionary<string, ZipNode> _empty = new();

    public ZipNode? Parent { get; set; }
    public string? Name { get; }
    public ZipEntry? ZipEntry { get; }
    public Dictionary<string, ZipNode> Children { get; }

    public string DebugName => Name ?? ZipEntry?.Name ?? "no name";

    public ZipNode(string name, IEnumerable<ZipEntry> entries)
    {
        Name = name;
        ZipEntry = null;
        Children = [];
        Parent = null;
        foreach (var entry in entries)
            Insert(entry);
    }

    public ZipNode(string name, ZipNode? parent = null)
    {
        Name = name;
        ZipEntry = null;
        Children = [];
        Parent = parent;
    }

    public ZipNode(ZipEntry file, ZipNode parent)
    {
        Name = null;
        ZipEntry = file;
        Children = _empty;
        Parent = parent;
    }

    public void Insert(ZipEntry zipEntry)
    {
        var current = this;
        var name = zipEntry.Name.AsSpan();

        foreach (var range in name.Split('\\'))
        {
            var part = name[range];
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(current.Children.GetAlternateLookup<ReadOnlySpan<char>>(), part, out var existed);

            if (range.End.Value == name.Length)
            {
                // If this is the last part, we're at the file
                value = new ZipNode(zipEntry, current);
                return;
            }

            if (!existed)
            {
                value = new ZipNode(part.ToString(), current);
            }

            current = value!;
        }
    }
}