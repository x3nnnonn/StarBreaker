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

    public void Insert(P4kEntry p4KEntry)
    {
        var current = this;
        var name = p4KEntry.Name.AsSpan();

        foreach (var range in name.Split('\\'))
        {
            var part = name[range];
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(current.Children.GetAlternateLookup<ReadOnlySpan<char>>(), part, out var existed);

            if (range.End.Value == name.Length)
            {
                // If this is the last part, we're at the file
                value = new P4kFileNode(p4KEntry, current);
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
    
    /// <summary>
    /// Transforms SOCPAK files into expandable SOCPAK nodes recursively.
    /// Should be called after the tree is fully built.
    /// </summary>
    public void TransformSocPakFiles(IP4kFile parentP4kFile)
    {
        var keysToUpdate = new List<string>();
        var nodesToReplace = new List<(string key, IP4kNode newNode)>();
        
        foreach (var (key, child) in Children)
        {
            if (child is P4kDirectoryNode childDir)
            {
                // Recursively transform children
                childDir.TransformSocPakFiles(parentP4kFile);
            }
            else if (child is P4kFileNode fileNode && P4kSocPakFileNode.IsSocPakFile(fileNode.P4KEntry.Name))
            {
                // Replace SOCPAK file with expandable SOCPAK node
                var socPakNode = new P4kSocPakFileNode(fileNode.P4KEntry, this, parentP4kFile);
                nodesToReplace.Add((key, socPakNode));
            }
        }
        
        // Apply replacements
        foreach (var (key, newNode) in nodesToReplace)
        {
            Children[key] = newNode;
        }
    }

    // This is probably suboptimal, but when we do this we'll be doing
    // a lot of IO anyway so it doesn't really matter
    public IEnumerable<P4kEntry> CollectEntries()
    {
        foreach (var child in Children.Values)
        {
            switch (child)
            {
                case P4kDirectoryNode directoryNode:
                    foreach (var entry in directoryNode.CollectEntries())
                        yield return entry;
                    break;
                case P4kFileNode fileNode:
                    yield return fileNode.P4KEntry;
                    break;
                case P4kSocPakFileNode socPakNode:
                    yield return socPakNode.P4KEntry;
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}