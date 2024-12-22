using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[DebuggerDisplay("{Name}")]
public sealed class ZipNode
{
    private static readonly Dictionary<string, ZipNode> _empty = new();

    public string? Name { get; }
    public ZipEntry? ZipEntry { get; }
    public Dictionary<string, ZipNode> Children { get; }

    public ZipNode(string name)
    {
        Name = name;
        ZipEntry = null;
        Children = [];
    }

    public ZipNode(ZipEntry file)
    {
        Name = null;
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
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(current.Children.GetAlternateLookup<ReadOnlySpan<char>>(), part, out var existed);

            if (range.End.Value == name.Length)
            {
                // If this is the last part, we're at the file
                value = new ZipNode(zipEntry);
                return;
            }

            if (!existed)
            {
                value = new ZipNode(part.ToString());
            }

            current = value!;
        }
    }
}