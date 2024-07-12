using StarBreaker.P4k;

namespace StarBreaker.Profile;

public class ZipNode
{
    private readonly Dictionary<int, ZipNode> _directory;
    private readonly List<ZipEntry> _files;
    private readonly string _name;
    
    public string Name => _name;
    public IDictionary<int, ZipNode> Directory => _directory;
    public List<ZipEntry> Files => _files;

    public ZipNode(string name)
    {
        _name = name;
        _files = [];
        _directory = [];
    }

    public ZipNode(ReadOnlySpan<ZipEntry> zipEntries)
    {
        _name = "";
        _directory = [];
        _files = [];
        
        Span<Range> ranges = new Range[20];
        foreach (var zipEntry in zipEntries)
        {
            var current = this;
            var name = zipEntry.Name.AsSpan();
            var rangeCount = name.Split(ranges, '\\');

            for (var index = 0; index < rangeCount - 1; index++)
            {
                var part = name[ranges[index]];
                var partHashCode = string.GetHashCode(part);
                
                if (!current._directory.TryGetValue(partHashCode, out var value))
                {
                    value = new ZipNode(part.ToString());
                    current._directory[partHashCode] = value;
                }

                current = value;
            }
            
            current._files.Add(zipEntry);
        }
    }

    public override string ToString()
    {
        return $"{Name} | {Directory.Count}";
    }
}