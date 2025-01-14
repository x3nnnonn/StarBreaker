using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarBreaker.P4k;

[DebuggerDisplay("{Name}")]
public sealed class P4kDirectoryNode : IP4kNode
{
    private readonly IP4kNode? _parent;
    public IP4kNode Parent => _parent ?? throw new InvalidOperationException("You might have tried to get the parent of the root node");

    public string Name { get; }
    public Dictionary<string, IP4kNode> Children { get; }

    public P4kDirectoryNode(string name, IP4kNode parent)
    {
        Name = name;
        _parent = parent;
        Children = [];
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
                value = new P4kFileNode(zipEntry, current);
                return;
            }

            if (!existed)
            {
                value = new P4kDirectoryNode(part.ToString(), current);
            }

            if (value is not P4kDirectoryNode directoryNode)
                throw new InvalidOperationException("Expected a directory node");

            current = directoryNode;
        }
    }
}
