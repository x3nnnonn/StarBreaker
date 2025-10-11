using System.Diagnostics;
using StarBreaker.Common;
using StarBreaker.Dds;
using StarBreaker.P4k;

namespace StarBreaker.Extraction;

[DebuggerDisplay("{Name} - {_entries.Count} entries")]
public sealed class DdsFileNode : IP4kFileNode
{
    private readonly IP4kFile _p4k;
    private readonly List<P4kEntry> _entries;
    private ulong? _sizeCache;

    public string Name { get; }

    public string RelativeOutputPath
    {
        get
        {
            var shortestName = _entries.MinBy(x => x.RelativeOutputPath.Length);
            if (shortestName == null)
                throw new InvalidOperationException("DdsFileNode has no entries.");
            return shortestName.RelativeOutputPath;
        }
    }

    public ulong Size
    {
        get
        {
            _sizeCache ??= _entries.Aggregate<P4kEntry, ulong>(0, (acc, c) => acc + c.UncompressedSize);

            return _sizeCache.Value;
        }
    }

    public DdsFileNode(IP4kFile p4k, P4kEntry entry, string name)
    {
        _p4k = p4k;
        _entries = [entry];
        Name = name;
    }

    public Stream Open()
    {
        if (_entries.Count == 1)
            return _p4k.OpenStream(_entries[0]);

        var arrays = _entries.ToDictionary(x => x.Name.Split('\\').Last(), x => _p4k.OpenStream(x).ToArray());

        return DdsFile.MergeToArray(arrays);
    }

    public void AddEntry(P4kEntry entry) => _entries.Add(entry);
}