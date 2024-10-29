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
    
    public ZipNode(ZipEntry file)
    {
        _name = file.Name;
        _zipEntry = file;
        _children = _empty;
    }

    public void Insert(ZipEntry zipEntry)
    {
        Span<Range> ranges = stackalloc Range[20];

        var current = this;
        var name = zipEntry.Name.AsSpan();
        var rangeCount = name.Split(ranges, '\\');

        for (var index = 0; index < rangeCount; index++)
        {
            var part = name[ranges[index]];
            var partHashCode = string.GetHashCode(part);
            
            if (index == rangeCount - 1)
            {
                // If this is the last part, we're at the file
                current._children[partHashCode] = new ZipNode(zipEntry);
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
 }