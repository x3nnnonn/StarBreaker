using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using StarBreaker.Common;
using StarBreaker.FileSystem;
using StarBreaker.P4k;

namespace StarBreaker.Extraction;

[DebuggerDisplay("{Name}")]
public sealed class P4kDirectoryNode : IP4kDirectoryNode, IFileSystem
{
    public string Name { get; }
    public Dictionary<string, IP4kNode> Children { get; }

    private ulong? _sizeCache;

    public ulong Size
    {
        get
        {
            _sizeCache ??= Children.Values.Aggregate<IP4kNode, ulong>(0, (acc, c) => acc + c.Size);

            return _sizeCache.Value;
        }
    }

    public P4kDirectoryNode(string name)
    {
        Name = name;
        Children = new Dictionary<string, IP4kNode>(StringComparer.OrdinalIgnoreCase);
        _sizeCache = null;
    }

    private static void Insert(P4kDirectoryNode root, IP4kFile p4kFile, P4kEntry p4KEntry)
    {
        // Invalidate size cache as we're adding a new node
        root._sizeCache = null;

        var current = root;
        var name = p4KEntry.Name.AsSpan();

        foreach (var range in name.Split('\\'))
        {
            var part = name[range];

            if (range.End.Value == name.Length)
            {
                //we are a file
                var indexOfDds = part.IndexOf(".dds", StringComparison.InvariantCultureIgnoreCase);
                ReadOnlySpan<char> key;
                if (indexOfDds == -1)
                {
                    key = part;
                }
                else
                {
                    var baseNameEnd = indexOfDds + ".dds".Length;
                    key = part[..baseNameEnd];
                }

                ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(current.Children.GetAlternateLookup<ReadOnlySpan<char>>(), key, out var existed);
                if (existed)
                {
                    switch (value)
                    {
                        case P4kFileNode:
                            throw new InvalidOperationException("File already exists");
                        case DdsFileNode ddsFile:
                            ddsFile.AddEntry(p4KEntry);
                            return;
                        default:
                            throw new InvalidOperationException("Expected a file node");
                    }
                }

                var isSplitDds = key.EndsWith(".dds");

                if (isSplitDds)
                {
                    value = new DdsFileNode(p4kFile, p4KEntry, key.ToString());
                    continue;
                }

                var isArchive = part.EndsWith(".socpak", StringComparison.InvariantCultureIgnoreCase);

                if (isArchive)
                {
                    var isShaderCache = part.StartsWith("shadercache_", StringComparison.InvariantCultureIgnoreCase);
                    if (!isShaderCache)
                    {
                        value = FromP4k(P4kFile.FromP4kEntry(p4kFile, p4KEntry));
                        continue;
                    }
                }

                value = new P4kFileNode(p4KEntry, p4kFile, key.ToString());
            }
            else
            {
                //we are a directory
                ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(current.Children.GetAlternateLookup<ReadOnlySpan<char>>(), part, out var existed);
                if (!existed)
                    value = new P4kDirectoryNode(part.ToString());

                if (value is not P4kDirectoryNode directoryNode)
                    throw new InvalidOperationException("Expected a directory node");

                current = directoryNode;
            }
        }
    }

    public static P4kDirectoryNode FromP4k(IP4kFile file, IProgress<double>? progress = null)
    {
        progress?.Report(0.0);
        //only report every 1% 
        var reportInterval = Math.Max(file.Entries.Length / 100, 1);
        var root = new P4kDirectoryNode(file.Name);

        var entriesProcessed = 0;
        foreach (var entry in file.Entries)
        {
            Insert(root, file, entry);

            entriesProcessed++;
            if (entriesProcessed % reportInterval == 0)
                progress?.Report(entriesProcessed / (double)file.Entries.Length);
        }

        progress?.Report(1.0);

        return root;
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                yield break;

            if (value is not P4kDirectoryNode directory)
                yield break;

            current = directory;
        }

        foreach (var child in current.Children.Values.OfType<P4kDirectoryNode>())
        {
            yield return child.Name;
        }
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');

        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                yield break;

            if (value is not P4kDirectoryNode directory)
                yield break;

            current = directory;
        }

        foreach (var child in current.Children.Values.OfType<P4kFileNode>())
        {
            yield return child.P4KEntry.Name;
        }
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');

        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                yield break;

            if (value is not P4kDirectoryNode directory)
                yield break;

            current = directory;
        }

        foreach (var child in current.Children.Values.OfType<P4kFileNode>())
        {
            if (!FileSystemName.MatchesSimpleExpression(searchPattern, child.P4KEntry.Name.Split('\\').Last()))
                continue;

            yield return child.P4KEntry.Name;
        }
    }

    public bool FileExists(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                return false;

            if (value is P4kDirectoryNode directory)
                current = directory;
            else
                return value is P4kFileNode && index == partsCount - 1;
        }

        return false;
    }

    public Stream OpenRead(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                throw new FileNotFoundException();

            if (value is P4kDirectoryNode directory)
                current = directory;
            else if (value is P4kFileNode file && index == partsCount - 1)
                return file.Open();
            else
                throw new FileNotFoundException();
        }

        throw new FileNotFoundException();
    }

    public byte[] ReadAllBytes(string path)
    {
        Span<Range> ranges = stackalloc Range[20];
        var span = path.AsSpan();
        var partsCount = span.Split(ranges, '\\');
        var current = this;

        for (var index = 0; index < partsCount; index++)
        {
            var part = ranges[index];
            if (!current.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(span[part], out var value))
                throw new FileNotFoundException();

            if (value is P4kDirectoryNode directory)
                current = directory;
            else if (value is P4kFileNode file && index == partsCount - 1)
                return file.Open().ToArray();
            else
                throw new FileNotFoundException();
        }

        throw new FileNotFoundException();
    }

    public IEnumerable<IP4kFileNode> CollectAllFiles()
    {
        foreach (var child in Children.Values)
        {
            if (child is IP4kFileNode file)
            {
                yield return file;
            }
            else if (child is IP4kDirectoryNode dir)
            {
                foreach (var subFile in dir.CollectAllFiles())
                {
                    yield return subFile;
                }
            }
        }
    }
}